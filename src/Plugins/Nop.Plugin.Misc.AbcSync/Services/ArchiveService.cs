using Nop.Core.Domain.Messages;
using Nop.Data;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Messages;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Nop.Plugin.Misc.AbcSync
{
    public class ArchiveService : BaseAbcWarehouseService
    {
        private static readonly string _backendInvItemNum = "ITEM_NUMBER";
        private static readonly string _backendInvTable = "DA1_INVENTORY_MASTER";

        private static readonly string _deleteArchivedProductPictures = @"DELETE pic FROM [Picture] pic
	                JOIN Product_Picture_Mapping ppm ON pic.Id = ppm.PictureId JOIN Product p ON p.Id = ppm.ProductId
                    WHERE p.Deleted = 1";

        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IEmailSender _emailSender;
        private readonly ImportSettings _importSettings;
        private readonly INopDataProvider _nopDbContext;
        private readonly ISettingService _settingService;
        private readonly ILogger _logger;

        public ArchiveService(
            EmailAccountSettings emailAccountSettings,
            IEmailAccountService emailAccountService,
            IEmailSender emailSender,
            ImportSettings importSettings,
            INopDataProvider nopDbContext,
            ISettingService settingService,
            ILogger logger)
        {
            _emailAccountSettings = emailAccountSettings;
            _emailAccountService = emailAccountService;
            _emailSender = emailSender;
            _importSettings = importSettings;
            _nopDbContext = nopDbContext;
            _settingService = settingService;
            _logger = logger;
        }

        /// <summary>
        /// returns an IEnumberable of files in the directory that match the search pattern. will not search subdirectories
        /// </summary>
        /// <param name="path"></param>
        /// <param name="searchPattern"></param>
        /// <returns></returns>
        private IEnumerable<string> GetFileNames(string path, string searchPattern = "*")
        {
            return Directory.EnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly).Select(Path.GetFileName);
        }

        /// <summary>
        /// moves files from targetDirectory whose name does not start with an item number in allowedItemNumbers to an "Archive" directory in the target
        /// </summary>
        /// <param name="allowedItemNumbers"></param>
        /// <param name="targetDirectory"></param>
        /// <param name="fileNames">must start with an item number followed by '_'</param>
        private void ArchiveFiles(HashSet<string> allowedItemNumbers, string targetDirectory, IEnumerable<string> fileNames, ref HashSet<string> archivedItemNumbers, string archivePath = null)
        {
            var archiveDate = DateTime.Today;
            var archivePrefix = $"{archiveDate.Year}.{archiveDate.Month}.{archiveDate.Day}_";
            if (!targetDirectory.EndsWith(@"\"))
            {
                targetDirectory += @"\";
            }

            var archiveFolderPath = archivePath ?? targetDirectory + "Archive";
            if (!Directory.Exists(archiveFolderPath))
            {
                Directory.CreateDirectory(archiveFolderPath);
            }

            HashSet<string> filesToMove = new HashSet<string>();

            foreach (var fileName in fileNames)
            {
                var itemNumber = fileName.Split('_')[0];
                if (!allowedItemNumbers.Contains(itemNumber))
                {
                    filesToMove.Add(fileName);
                    archivedItemNumbers.Add(itemNumber);
                }
            }

            foreach (var file in filesToMove)
            {
                var archiveFilePath = Path.Combine(archiveFolderPath, String.Concat(archivePrefix, file));
                File.Move(Path.Combine(targetDirectory, file), archiveFilePath);
            }
        }

        /// <summary>
        /// moves old energy guides, product specs, and images to archive folders. Deletes pictures attached to deleted products
        /// </summary>
        /// <param name="backendConn">connection to the ISAM backend to check valid products</param>
        public void ArchiveProductContent(IDbConnection backendConn)
        {
            var allowedItemNumbers = GetAllowedItemNumbers(backendConn);

            var archivedItemsSet = new HashSet<string>();
            var ProcessedImageDirectory = $"{_importSettings.GetLocalPicturesDirectory()}";

            ArchiveFiles(allowedItemNumbers, _importSettings.GetEnergyGuidePdfPath(), GetFileNames(_importSettings.GetEnergyGuidePdfPath()), ref archivedItemsSet);
            ArchiveFiles(allowedItemNumbers, _importSettings.GetSpecificationPdfPath(), GetFileNames(_importSettings.GetSpecificationPdfPath()), ref archivedItemsSet);
            ArchiveFiles(allowedItemNumbers, ProcessedImageDirectory, GetFileNames(ProcessedImageDirectory, "*_large.*"),
                ref archivedItemsSet, $"{_importSettings.GetLocalPicturesDirectory()}/Archive");

            //deleting old pictures from archived products
            _nopDbContext.ExecuteNonQuery(_deleteArchivedProductPictures);

            var account = _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);
            var ccEmails = new List<string>();
            if (!string.IsNullOrEmpty(_importSettings.ArchiveTaskCCEmails))
            {
                ccEmails.AddRange(_importSettings.ArchiveTaskCCEmails.Split(','));
            }

            string body;
            bool hasArchivedItems = archivedItemsSet.Any();
            if (hasArchivedItems)
            {
                body = "No items were archived";
            }
            else
            {
                body = "Files from the following item numbers were archived:";
                foreach (var itemNo in archivedItemsSet)
                {
                    body += $"<br/>{itemNo}";
                }
            }

            _emailSender.SendEmail(account, "Archive Task Complete", body, account.Email, account.DisplayName, "johnjh@abcwarehouse.com", "", cc: ccEmails);

            if (hasArchivedItems)
            {
                _logger.Information($"Archived items: {string.Join(",", archivedItemsSet)}");
            }
            else
            {
                _logger.Information("No archived items.");
            }
        }


        private HashSet<string> GetAllowedItemNumbers(IDbConnection backendConn)
        {
            backendConn.Open();
            IDbCommand query = backendConn.CreateCommand();
            query.CommandText = $"SELECT DISTINCT Inv.{_backendInvItemNum} FROM {_backendInvTable} Inv;";

            var itemNoList = new List<string>();
            var itemNoReader = query.ExecuteReader();
            while (itemNoReader.Read())
            {
                var itemNum = itemNoReader[_backendInvItemNum] as string;
                if (!string.IsNullOrEmpty(itemNum))
                {
                    itemNoList.Add(itemNum);
                }
            }
            var allowedItemNumbers = new HashSet<string>(itemNoList);
            backendConn.Close();
            return allowedItemNumbers;
        }
    }
}