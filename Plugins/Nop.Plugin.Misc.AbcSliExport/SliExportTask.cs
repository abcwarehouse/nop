using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Stores;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Services.Stores;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System;
using System.Text.RegularExpressions;
using Nop.Data;
using System.Net;
using Nop.Services.Logging;
using Nop.Core.Domain.Media;
using Nop.Core;
using System.IO;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Microsoft.AspNetCore.Hosting;
using Nop.Core.Infrastructure;

namespace Nop.Plugin.Misc.AbcSliExport
{
    class SliExportTask : ISliExportTask
    {
        #region XML Tag Names

        // Grab the XML tag names from the web.config.
        private static readonly string _productListTag = "product_list";
		private static readonly string _productTag = "product";
		private static readonly string _brandTag = "brand";
		private static readonly string _nameTag = "name";
		private static readonly string _productUrlTag = "url";
        private static readonly string _itemNumberTag = "item_number";
        private static readonly string _nopIdTag = "nop-id";
        private static readonly string _skuTag = "sku";
        private static readonly string _modelDescTag = "model_desc";
        private static readonly string _shortDescTag = "short_desc";
		private static readonly string _longDescTag = "long_desc";
		private static readonly string _imageUrlTag = "image";
		private static readonly string _colorTag = "color";
		private static readonly string _rootCatTag = "category";
		private static readonly string _subCatListTag = "Sub_category";
		private static readonly string _priceTag = "price";
		private static readonly string _salePriceTag = "saleprice";
		private static readonly string _availabilityTag = "availability";
		private static readonly string _manNumTag = "mfn_Num";
        private static readonly string _featuresTag = "features";
        private static readonly string _featureTag = "feature";
        private static readonly string _promosTag = "promos";
        private static readonly string _promoTag = "promo";
      
        private string _isClearanceTag = "clearance";

        #endregion

        private readonly SliExportSettings _settings;
		private readonly Customer _searchCustomer;

		private readonly ISettingService _settingService;
		private readonly IManufacturerService _manufacturerService;
		private readonly IUrlRecordService _urlRecordService;
		private readonly IStoreMappingService _storeMappingService;
		private readonly IPriceCalculationService _priceCalcService;
		private readonly ICategoryService _categoryService;
		private readonly IPictureService _pictureService;
		private readonly ICustomerService _customerService;
        private readonly IRepository<ProductAbcDescription> _productAbcDescriptionRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<ProductCartPrice> _productCartPriceRepository;
        private readonly ILogger _logger;
        private readonly INopDataProvider _nopDbContext;
        private readonly MediaSettings _mediaSettings;
        private readonly FrontEndService _frontendService;
		private readonly ISpecificationAttributeService _specificationAttributeService;
		private readonly IStoreService _storeService;
		private readonly IAbcPromoService _abcPromoService;

        public SliExportTask(
			ISettingService settingService,
			IManufacturerService manufacturerService,
			IUrlRecordService urlRecordService,
			IStoreMappingService storeMappingService,
			IPriceCalculationService priceCalcService,
			ICategoryService categoryService,
			IPictureService pictureService,
			ICustomerService customerService,
            IRepository<ProductAbcDescription> productAbcDescriptionRepository,
            IRepository<Product> productRepository,
            IRepository<ProductCartPrice> productCartPriceRepository,
            ILogger logger,
            INopDataProvider nopDbContext,
            MediaSettings mediaSettings,
            FrontEndService frontendService,
			ISpecificationAttributeService specificationAttributeService,
			IStoreService storeService,
			IAbcPromoService abcPromoService
		)
		{
			_settingService = settingService;
			_manufacturerService = manufacturerService;
			_urlRecordService = urlRecordService;
			_storeMappingService = storeMappingService;
			_priceCalcService = priceCalcService;
			_categoryService = categoryService;
			_pictureService = pictureService;
			_customerService = customerService;
            _productAbcDescriptionRepository = productAbcDescriptionRepository;
            _productRepository = productRepository;
            _productCartPriceRepository = productCartPriceRepository;
            _logger = logger;
            _nopDbContext = nopDbContext;
            _mediaSettings = mediaSettings;

			_settings = _settingService.LoadSetting<SliExportSettings>();
			_searchCustomer = _customerService.InsertGuestCustomer();

            _frontendService = frontendService;
			_specificationAttributeService = specificationAttributeService;
			_storeService = storeService;
			_abcPromoService = abcPromoService;
		}

		public void Execute()
        {
			if (string.IsNullOrWhiteSpace(_settings.XMLFilename))
			{
				throw new NopException("SLI XML Filename not defined.");
			}
			if (!_settings.IsExportSelected)
			{
				throw new NopException("No export store is selected, will not be able to generate XML file.");
			}

            this.LogStart();
            XmlWriterSettings xmlSettings = InitializeXmlWriter();
			
			var env = EngineContext.Current.Resolve<IWebHostEnvironment>();
			var sliXmlPath = Path.Combine(env.ContentRootPath, _settings.XMLFilename);
 
            // Begin the XML document.
            using (XmlWriter xml = XmlWriter.Create(sliXmlPath, xmlSettings))
            {
                xml.WriteStartDocument();
                xml.WriteStartElement(_productListTag);

                // Grab all products and iterate through them.
                // Exporting all needed information into the XML.
                var products = _productRepository.Table.Where(p => !p.Deleted && p.Published).ToList();
                foreach (Product product in products)
                {
                    ExportProduct(xml, product);
                }

                // End the XML document.
                xml.WriteFullEndElement();
                xml.WriteEndDocument();
                xml.Close();
            }

			if (_settings.IsFTPEnabled)
			{
				SendFileViaFTP(sliXmlPath);
			}
			else
			{
				_logger.Warning("FTP Export disabled, generated file will not be sent.");
			}

            this.LogEnd();
        }

		private void SendFileViaFTP(string filepath)
		{
			if (string.IsNullOrWhiteSpace(_settings.FTPUsername) || string.IsNullOrWhiteSpace(_settings.FTPPassword))
			{
				throw new NopException("SLI FTP Credentials are empty, please add in plugin settings.");
			}

			if (string.IsNullOrWhiteSpace(_settings.FTPServer) || string.IsNullOrWhiteSpace(_settings.FTPPath))
			{
				throw new NopException("SLI FTP server and path are empty, please add in plugin settings.");
			}

			//push the generated file to sli via ftp
			using (WebClient client = new WebClient())
			{
				try
				{
					client.Credentials = new NetworkCredential(_settings.FTPUsername, _settings.FTPPassword);
					client.UploadFile(_settings.FTPServer + _settings.FTPPath, "STOR", filepath);
				}
				catch (WebException ex)
				{
					_logger.Error("Unable to upload generated export file", ex);
				}
			}
		}

		private static XmlWriterSettings InitializeXmlWriter()
        {
			XmlWriterSettings xmlSettings = new XmlWriterSettings
			{
				Async = false,
				CheckCharacters = true,
				ConformanceLevel = ConformanceLevel.Document,
				Encoding = Encoding.UTF8,
				Indent = true,
				IndentChars = "\t"
			};
			return xmlSettings;
        }

        #region Export Methods

        /// <summary>
        ///		Write to the XmlWriter all needed element data for the product.
        /// </summary>
        /// <param name="xml">
        ///		This XmlWriter should already be prepared.
        ///		This method is responsible for populating the product element.
        /// </param>
        /// <param name="product">
        ///		This is product from which this exports data.
        /// </param>
        private void ExportProduct(XmlWriter xml, Product product)
		{
			// Skip products that are not on at least one store
			// that is being exported.
			Store store = GetProductStore(product);
			if (store == null)
			{
				return;
			}

			// Now write the XML export elements.
			xml.WriteStartElement(_productTag);

            if (store.Url.Contains("hawthorne")) {
                WriteElementHelper(xml, _isClearanceTag, "Y");
            } else
            {
                WriteElementHelper(xml, _isClearanceTag, "N");
            }

			ExportBrand(xml, product);
			WriteElementHelper(xml, _nameTag, product.Name);
			ExportUrl(xml, product, store);
            ExportItemNumberAndModelDesc(xml, product);
            WriteElementHelper(xml, _nopIdTag, product.Id.ToString());
            WriteElementHelper(xml, _skuTag, product.Sku);
			WriteElementHelper(xml, _shortDescTag, StripMarkupForShortDescription(product.ShortDescription));
			WriteElementHelper(xml, _longDescTag, product.FullDescription);
			ExportPictureUrl(xml, product);
			ExportColor(xml, product);
			ExportCategories(xml, product);
			ExportPrices(xml, product);
			ExportAvailability(xml, product);
			WriteElementHelper(xml, _manNumTag, product.ManufacturerPartNumber);
            ExportSpecifications(xml, product);
            ExportPromo(xml, product, store.Url);
            ExportOpenBoxPrices(xml, product);

            xml.WriteFullEndElement();
			return;
		}

        private void ExportOpenBoxPrices(XmlWriter xml, Product product)
        {
            var productFlag = _frontendService.GetProductFlag(product.Id);

            if (productFlag == null || !productFlag.IsOpenBox()) { return; }

            var price = productFlag.CalculateOpenBoxPrice(product.Price);

            WriteElementHelper(xml, "open_box_price", price.ToString());
        }

        private void ExportPromo(XmlWriter xml, Product product, String URL)
        {
            var promos = _abcPromoService.GetActivePromosByProductId(product.Id);
			if (!promos.Any()) return;

            xml.WriteStartElement(_promosTag);

            foreach (var promo in promos)
            {
                xml.WriteStartElement(_promoTag);
                xml.WriteAttributeString("name", XmlConvert.EncodeName(promo.Description));
                xml.WriteValue(XmlSanitize(URL + promo.GetPdfPath()));
                xml.WriteEndElement();

            }
            xml.WriteEndElement();
        }

        private void ExportSpecifications(XmlWriter xml, Product product)
        {
            var features = _specificationAttributeService.GetProductSpecificationAttributes(product.Id)
				.Where(psa => (
					_specificationAttributeService.GetSpecificationAttributeOptionById(psa.Id) != null &&
					_specificationAttributeService.GetSpecificationAttributeById(
					_specificationAttributeService.GetSpecificationAttributeOptionById(psa.Id).Id).Name != "Color"
						&&
					_specificationAttributeService.GetSpecificationAttributeOptionById(psa.Id) != null &&
					_specificationAttributeService.GetSpecificationAttributeById(
						_specificationAttributeService.GetSpecificationAttributeOptionById(psa.Id).Id).Name != "Category")
				)
                .ToList();

            if (!features.Any())
            {
                return;
            }

            xml.WriteStartElement(_featuresTag);

            foreach(var feature in features)
            {
                xml.WriteStartElement(_featureTag);
                xml.WriteAttributeString("name", XmlConvert.EncodeName(
					_specificationAttributeService.GetSpecificationAttributeById(
						_specificationAttributeService.GetSpecificationAttributeOptionsBySpecificationAttribute(feature.Id)[0].SpecificationAttributeId
					).Name
				));
                xml.WriteValue(XmlSanitize(
					_specificationAttributeService.GetSpecificationAttributeById(
						_specificationAttributeService.GetSpecificationAttributeOptionsBySpecificationAttribute(feature.Id)[0].SpecificationAttributeId
					).Name
				));
                xml.WriteEndElement();
            }

            xml.WriteEndElement();
        }

        private void ExportItemNumberAndModelDesc(XmlWriter xml, Product product)
        {
            var prodAbcDesc = _productAbcDescriptionRepository.Table.Where(pad => pad.Product_Id == product.Id).FirstOrDefault();

            var hasAbcDesc = false;
            if (prodAbcDesc != null)
            {
                if(!string.IsNullOrEmpty(prodAbcDesc.AbcItemNumber))
				{
					var productFlag = _frontendService.GetProductFlag(product.Id);
					var itemNumber = $"{prodAbcDesc.AbcItemNumber}{(productFlag != null && productFlag.IsSpecialOrder() ? "+" : "" ?? "")}"; 
					WriteElementHelper( xml, _itemNumberTag, itemNumber);
				}

                hasAbcDesc = !string.IsNullOrEmpty(prodAbcDesc.AbcDescription);
            }

            if (hasAbcDesc)
            {
                WriteElementHelper(xml, _modelDescTag, prodAbcDesc.AbcDescription);
            }
            else
            {
                WriteElementHelper(xml, _modelDescTag, product.ShortDescription);
            }

            return;
        }

        /// <summary>
        ///		Get the store that the given product is on,
        ///		limited by the export settings.
        ///		If it is multiple, the one with the smallest ID is given.
        ///		If it is not on any after limiting, then NULL is returned.
        /// </summary>
        /// <returns>
        ///		NULL means none found given settings.
        ///		Otherwise, the store that the given product is on.
        /// </returns>
        private Store GetProductStore(Product product)
		{
			// Find all store mappings to this product.
			// Limit by the given export settings.
			// Use the URL of (only) the first store that meets all the criteria.
			var storeList = _storeMappingService
				.GetStoreMappings(product)
				.OrderBy(sm => _storeService.GetStoreById(sm.StoreId).DisplayOrder)
					.ThenBy(sm => sm.StoreId);
			foreach (var storeMapping in storeList)
			{
				var store = _storeService.GetStoreById(storeMapping.StoreId);

				if (_settings.ExportAbcWarehouse &&
					(store.Name.Contains("ABC")))
				{
					return store;
				}
                if (_settings.ExportHawthorne &&
                    (store.Name.Contains("Hawthorne")))
                {
                    return store;
                }
            }

			return null;
		}

		/// <summary>
		///		Write to the XmlWriter all needed element data for the brand.
		/// </summary>
		/// <param name="xml">
		///		This XmlWriter should already be prepared.
		///		This method is responsible for populating the brand element.
		/// </param>
		/// <param name="product">
		///		This is product from which this exports data.
		/// </param>
		private void ExportBrand(XmlWriter xml, Product product)
		{
			// Get the manufacturer for this product.
			// If there are multiple,
			// it uses the manufacturer's display order and ID.
			var manufacturerMapping = _manufacturerService
				.GetProductManufacturersByProductId(product.Id, true)
				.Where(p => _manufacturerService.GetManufacturerById(p.ManufacturerId) != null &&
							_manufacturerService.GetManufacturerById(p.ManufacturerId).Published)
				.OrderBy(p => _manufacturerService.GetManufacturerById(p.ManufacturerId).DisplayOrder)
					.ThenBy(p => p.ManufacturerId)
				.FirstOrDefault();
			if (manufacturerMapping != null)
			{
				WriteElementHelper(
					xml, _brandTag, _manufacturerService.GetManufacturerById(manufacturerMapping.ManufacturerId).Name);
            }

			return;
		}

		/// <summary>
		///		Write to the XmlWriter all needed element data for the URL.
		/// </summary>
		/// <param name="xml">
		///		This XmlWriter should already be prepared.
		///		This method is responsible for populating the URL element.
		/// </param>
		/// <param name="product">
		///		This is product from which this exports data.
		/// </param>
		private void ExportUrl(XmlWriter xml, Product product, Store store)
		{
			string slug = _urlRecordService.GetActiveSlug(product.Id, "Product", 0);
			WriteElementHelper(xml, _productUrlTag, store.Url + slug);
			
			return;
		}

		/// <summary>
		///		Write to the XmlWriter all needed element data for the picture URL.
		/// </summary>
		/// <param name="xml">
		///		This XmlWriter should already be prepared.
		///		This method is responsible for populating the picture URL element.
		/// </param>
		/// <param name="product">
		///		This is product from which this exports data.
		/// </param>
		private void ExportPictureUrl(XmlWriter xml, Product product)
		{
			var pic = _pictureService.GetPicturesByProductId(product.Id, 1).FirstOrDefault();
			if (pic != null)
			{
				WriteElementHelper(xml, _imageUrlTag,
					_pictureService.GetPictureUrl(pic.Id, _mediaSettings.ProductThumbPictureSize));
                //_nopDbContext.Detach(pic);
            }

			return;
		}

		/// <summary>
		///		Write to the XmlWriter all needed element data for the color.
		/// </summary>
		/// <param name="xml">
		///		This XmlWriter should already be prepared.
		///		This method is responsible for populating the picture URL element.
		/// </param>
		/// <param name="product">
		///		This is product from which this exports data.
		/// </param>
		private void ExportColor(XmlWriter xml, Product product)
		{
			// Get the product specificiation attribute
			// that relates to the specification attribute
			// with the name "Color".
			// If there are more than one,
			// take the specification attribute with the smaller ID.
			
			var productSpecAttr = _specificationAttributeService.GetProductSpecificationAttributes(product.Id)
				.Where(psa => _specificationAttributeService.GetSpecificationAttributeOptionById(psa.Id) != null &&
						_specificationAttributeService.GetSpecificationAttributeById(
						_specificationAttributeService.GetSpecificationAttributeOptionById(psa.Id).Id).Name != "Color")
				.OrderBy(psa =>
						_specificationAttributeService.GetSpecificationAttributeById(
						_specificationAttributeService.GetSpecificationAttributeOptionById(psa.Id).Id).Id)
				.FirstOrDefault();

			// If no product specification attribute was found
			// this product does not have a color related to it.
			if (productSpecAttr == null)
			{
				return;
			}
			
			// If the product specification attribute uses a custom value, use that.
			if (productSpecAttr.AttributeType == SpecificationAttributeType.CustomText)
			{
				WriteElementHelper(xml, _colorTag, productSpecAttr.CustomValue);
				return;
			}

			// Otherwise, use the specification attribute option.
			WriteElementHelper(
				xml, _colorTag, _specificationAttributeService.GetSpecificationAttributeOptionsBySpecificationAttribute(productSpecAttr.Id)[0].Name);

            //_nopDbContext.Detach(productSpecAttr);
			return;
		}

		/// <summary>
		///		Write to the XmlWriter all needed element data for the categories.
		/// </summary>
		/// <param name="xml">
		///		This XmlWriter should already be prepared.
		///		This method is responsible for populating the categories element.
		/// </param>
		/// <param name="product">
		///		This is product from which this exports data.
		/// </param>
		private void ExportCategories(XmlWriter xml, Product product)
		{
			Queue<Category> categories = new Queue<Category>();
			List<Category> allExportCategories = new List<Category>();

			foreach (var cat in _categoryService.GetProductCategoriesByProductId(product.Id))
			{
				var category = _categoryService.GetCategoryById(cat.CategoryId);
				categories.Enqueue(category);
				allExportCategories.Add(category);
			}

			List<Category> rootCats = new List<Category>();
			string subCats = string.Empty;

			while (categories.Count > 0)
			{
				var cat = categories.Dequeue();

				if (cat.ParentCategoryId == 0)
				{
					// This is a root category.
					// Add it to the list of roots to be dealt with later.
					rootCats.Add(cat);

					continue;
				}

				// This is a sub-category.
				subCats = CategoryAppendHelper(subCats, cat.Name);

				// Also add the parent of this sub-category
				// as long as it does not already exist in the queue.
				var parent = _categoryService.GetCategoryById(cat.ParentCategoryId);
				if (!allExportCategories.Contains(parent))
				{
					categories.Enqueue(parent);
					allExportCategories.Add(parent);
				}
			}

			// Get the "root" category, and then add the rest to subCats.
			// This ensures there is only one "root".
			Category rootCat =
				rootCats.Find(c1 => c1.Id == rootCats.Min(c2 => c2.Id));
			foreach (var root in rootCats)
			{
				if (root == rootCat)
				{
					continue;
				}
				CategoryAppendHelper(subCats, root.Name);
			}

			WriteElementHelper(xml, _rootCatTag,
				(rootCat == null) ? string.Empty : rootCat.Name);
			WriteElementHelper(xml, _subCatListTag, subCats);

			return;
		}

		/// <summary>
		///		Write to the XmlWriter all needed element data for the prices.
		/// </summary>
		/// <param name="xml">
		///		This XmlWriter should already be prepared.
		///		This method is responsible for populating the price elements.
		/// </param>
		/// <param name="product">
		///		This is product from which this exports data.
		/// </param>
		private void ExportPrices(XmlWriter xml, Product product)
		{
			decimal price = product.OldPrice;
			decimal salePrice = _priceCalcService.GetFinalPrice(
				product, _searchCustomer, 0, true, 1);

			WriteElementHelper(xml, _priceTag, price.ToString());
			WriteElementHelper(xml, _salePriceTag, salePrice.ToString());

            var cartProduct = _productCartPriceRepository.Table.Where(pcp => pcp.Product_Id == product.Id).FirstOrDefault();
            if(cartProduct != null)
            {
                WriteElementHelper(xml, "MAPPricing", true.ToString());
            }

            if (product.CallForPrice)
            {
                WriteElementHelper(xml, "Call_Price", true.ToString());
            }



            return;
		}

		/// <summary>
		///		Write to the XmlWriter all needed element data for the availability.
		/// </summary>
		/// <param name="xml">
		///		This XmlWriter should already be prepared.
		///		This method is responsible for populating the availibility element.
		/// </param>
		/// <param name="product">
		///		This is product from which this exports data.
		/// </param>
		private void ExportAvailability(XmlWriter xml, Product product)
		{
			string avail = "1";

			if (product.DisableBuyButton)
			{
				avail = "0";
			}

			WriteElementHelper(xml, _availabilityTag, avail);
			return;
		}

        

        #region Export Helpers

        /// <summary>
        ///		Write out the element to the provided XML
        ///		if and only if both the tag and value are non-empty.
        /// </summary>
        private void WriteElementHelper(XmlWriter xml, string tag, string value)
		{
			if (string.IsNullOrWhiteSpace(value) ||
				string.IsNullOrWhiteSpace(tag))
			{
				return;
			}

			xml.WriteElementString(XmlSanitize(tag), XmlSanitize(value));
			return;
		}

		/// <summary>
		///		Add the new category to the old category listing.
		///		This will add in the delimiter only when needed.
		/// </summary>
		/// <returns>
		///		The result of the appending.
		/// </returns>
		private string CategoryAppendHelper(string oldCategories, string newCategory)
		{
			if (string.IsNullOrWhiteSpace(oldCategories))
			{
				return newCategory;
			}
			if (string.IsNullOrWhiteSpace(newCategory))
			{
				return oldCategories;
			}

			return newCategory + "|" + oldCategories;
		}

        #endregion

        #endregion
        //sanitize output for xml
        static string XmlSanitize(string txt)
        {
            string r = "[\x00-\x08\x0B\x0C\x0E-\x1F\x26]";
            return Regex.Replace(txt, r, "", RegexOptions.Compiled);
        }


        private string StripMarkupForShortDescription(string input)
        {
            if (input == null) return null;

            const string identifier = "<span style=\"Times New Roman&quot;\">";
            const string endIdentifier = "</span>";

            if (input.Contains(identifier))
            {
                int identifierId = input.IndexOf(identifier);
                int endIdentifierId = input.IndexOf(endIdentifier);

                int length = endIdentifierId - identifierId - identifier.Length;
                string output = input.Substring(identifierId + identifier.Length, length).Replace("&quot;", "\"").Replace("&trade;", "™").Replace("<br />", "");

                return output;
            }

            return input;
        }
    }
}