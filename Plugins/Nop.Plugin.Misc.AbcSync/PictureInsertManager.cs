using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.AspNetCore.StaticFiles;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore;

namespace Nop.Plugin.Misc.AbcSync
{
    public class PictureInsertManager
    {
        private readonly int PICTURE_BUFFER_SIZE = 50;

        private readonly INopDataProvider _nopDbContext;
        private int _nInserts;
        private List<ProductPicture> _productPictures;
        private List<TempPictureInput> _batchedPictureInputs;

        private readonly string _nopSqlConnectionString =
            DataSettingsManager.LoadSettings().ConnectionString;

        public PictureInsertManager(INopDataProvider nopDbContext)
        {
            _nopDbContext = nopDbContext;

            _nInserts = 0;
            _productPictures = new List<ProductPicture>();
            _batchedPictureInputs = new List<TempPictureInput>();
        }

        /// <summary>
        /// input for insert into picture table
        /// </summary>
        private class TempPictureInput
        {
            public byte[] PictureBytes { get; set; }
            public string MimeType { get; set; }
            public string SeoFilename { get; set; }
            public int ProductId { get; set; }
        }

        /// <summary>
        /// output from temp table about the inserts that were just committed
        /// </summary>
        private class TempPictureOutput
        {
            public int PictureId { get; set; }
            public int ProductId { get; set; }
        }

        public void Update(byte[] pictureBytes, string seoName, string url)
        {
            using (SqlConnection sqlConnection = new SqlConnection(_nopSqlConnectionString))
            {
                sqlConnection.Open();
                SqlCommand updateImageCommand = sqlConnection.CreateCommand();
                updateImageCommand.CommandText = "UPDATE Picture SET MimeType = @MimeType, IsNew = 1 WHERE SeoFilename = @SeoFilename;";
                new FileExtensionContentTypeProvider().TryGetContentType(url, out var contentType);
                updateImageCommand.Parameters.Add(new SqlParameter("MimeType", contentType));
                updateImageCommand.Parameters.Add(new SqlParameter("SeoFilename", seoName));
                updateImageCommand.ExecuteNonQuery();
                updateImageCommand.Dispose();

                SqlCommand selectImageIdCommand = sqlConnection.CreateCommand();
                selectImageIdCommand.CommandText = "SELECT Id from Picture WHERE SeoFilename = @SeoFilename;";
                selectImageIdCommand.Parameters.Add(new SqlParameter("SeoFilename", seoName));
                var pictureId = selectImageIdCommand.ExecuteScalar();
                selectImageIdCommand.Dispose();

                SqlCommand updateImageBinaryCommand = sqlConnection.CreateCommand();
                updateImageBinaryCommand.CommandText = $"UPDATE PictureBinary SET BinaryData = @PictureBytes WHERE PictureId = {pictureId};";
                updateImageBinaryCommand.Parameters.Add(new SqlParameter("PictureBytes", pictureBytes));
                updateImageBinaryCommand.ExecuteNonQuery();
                updateImageBinaryCommand.Dispose();
            }
        }

        public void Insert(byte[] pictureBytes, string seoName, string url, Product product)
        {
            new FileExtensionContentTypeProvider().TryGetContentType(url, out var contentType);
            _batchedPictureInputs.Add(new TempPictureInput
            {
                PictureBytes = pictureBytes,
                MimeType = contentType,
                SeoFilename = seoName,
                ProductId = product.Id
            });
            ++_nInserts;

            if (_nInserts >= PICTURE_BUFFER_SIZE)
            {
                Flush();
            }
        }

        public void Flush()
        {
            // connect to db
            using (SqlConnection sqlConnection = new SqlConnection(_nopSqlConnectionString))
            {
                sqlConnection.Open();
                // create temp table to store picture & product mapping
                SqlCommand tempTableDeclare = sqlConnection.CreateCommand();
                tempTableDeclare.CommandText = "CREATE TABLE #PictureOutputTemp (ProductId INT, PictureId INT)";
                tempTableDeclare.ExecuteNonQuery();
                tempTableDeclare.Dispose();

                // parameterized insert into picture table
                foreach (var pictureInput in _batchedPictureInputs)
                {
                    SqlCommand pictureInsert = sqlConnection.CreateCommand();
                    pictureInsert.CommandText
                        = "INSERT INTO Picture(MimeType, SeoFilename, IsNew)"
                        + " OUTPUT @ProductId, INSERTED.Id INTO #PictureOutputTemp"
                        + " VALUES(@MimeType, @SeoFilename, 0);";
                    pictureInsert.Parameters.Add(new SqlParameter("ProductId", pictureInput.ProductId));
                    pictureInsert.Parameters.Add(new SqlParameter("MimeType", pictureInput.MimeType));
                    pictureInsert.Parameters.Add(new SqlParameter("SeoFilename", pictureInput.SeoFilename));
                    pictureInsert.ExecuteNonQuery();
                    pictureInsert.Dispose();

                    SqlCommand selectImageIdCommand = sqlConnection.CreateCommand();
                    selectImageIdCommand.CommandText = "SELECT Id from Picture WHERE SeoFilename = @SeoFilename;";
                    selectImageIdCommand.Parameters.Add(new SqlParameter("SeoFilename", pictureInput.SeoFilename));
                    var pictureId = selectImageIdCommand.ExecuteScalar();
                    selectImageIdCommand.Dispose();

                    SqlCommand pictureBinaryInsert = sqlConnection.CreateCommand();
                    pictureBinaryInsert.CommandText
                        = "INSERT INTO PictureBinary (PictureId, BinaryData)"
                        + $" VALUES({pictureId}, @PictureBytes);";
                    pictureBinaryInsert.Parameters.Add(new SqlParameter("PictureBytes", pictureInput.PictureBytes));
                    pictureBinaryInsert.ExecuteNonQuery();
                    pictureBinaryInsert.Dispose();
                }

                // get all product picture mappings from the temp table
                SqlCommand getTempTableOutput = sqlConnection.CreateCommand();
                getTempTableOutput.CommandText = "SELECT * FROM #PictureOutputTemp;";
                SqlDataReader reader = getTempTableOutput.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        _productPictures.Add(new ProductPicture { ProductId = reader.GetInt32(0), PictureId = reader.GetInt32(1) });
                    }
                }
                reader.Close();
                getTempTableOutput.Dispose();
                sqlConnection.Close();
            }
            _batchedPictureInputs.Clear();
            _nInserts = 0;
        }

        public void FlushProductPictures(IRepository<ProductPicture> productPictureRepository)
        {
            EntityManager<ProductPicture> productPictureManager =
                new EntityManager<ProductPicture>(productPictureRepository);
            foreach (var productPicture in _productPictures)
            {
                productPictureManager.Insert(productPicture);
            }
            productPictureManager.Flush();
            _productPictures.Clear();
        }
    }
}