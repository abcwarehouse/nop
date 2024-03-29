﻿
using Microsoft.AspNetCore.Hosting;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcSync.Models;
using System;
using System.Configuration;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.IO;

namespace Nop.Plugin.Misc.AbcSync
{
    public class ImportSettings : ISettings
    {
        public string ArchiveTaskCCEmails { get; private set; }
        public string CatalogUpdateFailureCCEmails { get; private set; }
        public string MainStoreWebTestSubscriptionId { get; private set; }
        public string MainStoreWebTestResourceGroup { get; private set; }
        public string MainStoreWebTestName { get; private set; }
        public string ClearanceStoreWebTestSubscriptionId { get; private set; }
        public string ClearanceStoreWebTestResourceGroup { get; private set; }
        public string ClearanceStoreWebTestName { get; private set; }
        public string StagingDbConnectionString { get; private set; }
        public bool ImportABCStores { get; private set; }
        public bool ImportHawthorneStores { get; private set; }
        public bool SkipOldMattressesImport { get; private set; }


        // Sync Steps Skips
        public bool SkipFillStagingProductsTask { get; private set; }
        public bool SkipFillStagingPricingTask { get; private set; }
        public bool SkipFillStagingAccessoriesTask { get; private set; }
        public bool SkipFillStagingBrandsTask { get; private set; }
        public bool SkipFillStagingProductCategoryMappingsTask { get; private set; }
        public bool SkipFillStagingScandownEndDatesTask { get; private set; }
        public bool SkipFillStagingWarrantiesTask { get; private set; }

        public bool SkipImportProductsTask { get; private set; }
        public bool SkipMapCategoriesTask { get; private set; }
        public bool SkipImportProductCategoryMappingsTask { get; private set; }
        public bool SkipAddHomeDeliveryAttributesTask { get; private set; }
        public bool SkipImportMarkdownsTask { get; private set; }
        public bool SkipImportRelatedProductsTask { get; private set; }
        public bool SkipImportWarrantiesTask { get; private set; }
        public bool SkipUnmapNonstockClearanceTask { get; private set; }
        public bool SkipMapCategoryStoresTask { get; private set; }
        public bool SkipSliExportTask { get; private set; }

        public bool SkipImportDocumentsTask { get; private set; }
        public bool SkipImportIsamSpecsTask { get; private set; }
        public bool SkipImportFeaturedProductsTask { get; private set; }
        public bool SkipImportProductFlagsTask { get; private set; }
        public bool SkipImportSotPicturesTask { get; private set; }
        public bool SkipImportLocalPicturesTask { get; private set; }
        public bool SkipCleanDuplicateImagesTask { get; private set; }


        // Internal settings
        public DateTime LastPictureUpdate { get; set; }

        public ImportSettings FromModel(ImportModel model)
        {
            return new ImportSettings()
            {
                ArchiveTaskCCEmails = model.ArchiveTaskCCEmails,
                CatalogUpdateFailureCCEmails = model.CatalogUpdateFailureCCEmails,
                MainStoreWebTestSubscriptionId = model.MainStoreWebTestSubscriptionId,
                MainStoreWebTestResourceGroup = model.MainStoreWebTestResourceGroup,
                MainStoreWebTestName = model.MainStoreWebTestName,
                ClearanceStoreWebTestSubscriptionId = model.ClearanceStoreWebTestSubscriptionId,
                ClearanceStoreWebTestResourceGroup = model.ClearanceStoreWebTestResourceGroup,
                ClearanceStoreWebTestName = model.ClearanceStoreWebTestName,
                StagingDbConnectionString = model.StagingDbConnectionString,
                ImportABCStores = model.ImportABCStores,
                ImportHawthorneStores = model.ImportHawthorneStores,
                SkipOldMattressesImport = model.SkipOldMattressesImport,
                LastPictureUpdate = LastPictureUpdate,
                SkipFillStagingAccessoriesTask = model.SkipFillStagingAccessoriesTask,
                SkipFillStagingBrandsTask = model.SkipFillStagingBrandsTask,
                SkipFillStagingPricingTask = model.SkipFillStagingPricingTask,
                SkipFillStagingProductCategoryMappingsTask = model.SkipFillStagingProductCategoryMappingsTask,
                SkipFillStagingProductsTask = model.SkipFillStagingProductsTask,
                SkipFillStagingScandownEndDatesTask = model.SkipFillStagingScandownEndDatesTask,
                SkipFillStagingWarrantiesTask = model.SkipFillStagingWarrantiesTask,
                SkipImportProductsTask = model.SkipImportProductsTask,
                SkipMapCategoriesTask = model.SkipMapCategoriesTask,
                SkipImportProductCategoryMappingsTask = model.SkipImportProductCategoryMappingsTask,
                SkipAddHomeDeliveryAttributesTask = model.SkipAddHomeDeliveryAttributesTask,
                SkipImportMarkdownsTask = model.SkipImportMarkdownsTask,
                SkipImportRelatedProductsTask = model.SkipImportRelatedProductsTask,
                SkipImportWarrantiesTask = model.SkipImportWarrantiesTask,
                SkipUnmapNonstockClearanceTask = model.SkipUnmapNonstockClearanceTask,
                SkipMapCategoryStoresTask = model.SkipMapCategoryStoresTask,
                SkipImportDocumentsTask = model.SkipImportDocumentsTask,
                SkipImportIsamSpecsTask = model.SkipImportIsamSpecsTask,
                SkipImportFeaturedProductsTask = model.SkipImportFeaturedProductsTask,
                SkipImportProductFlagsTask = model.SkipImportProductFlagsTask,
                SkipImportSotPicturesTask = model.SkipImportSotPicturesTask,
                SkipImportLocalPicturesTask = model.SkipImportLocalPicturesTask,
                SkipCleanDuplicateImagesTask = model.SkipCleanDuplicateImagesTask,
                SkipSliExportTask = model.SkipSliExportTask
            };
        }

        public ImportModel ToModel()
        {
            return new ImportModel
            {
                ArchiveTaskCCEmails = ArchiveTaskCCEmails,
                CatalogUpdateFailureCCEmails = CatalogUpdateFailureCCEmails,
                MainStoreWebTestSubscriptionId = MainStoreWebTestSubscriptionId,
                MainStoreWebTestResourceGroup = MainStoreWebTestResourceGroup,
                MainStoreWebTestName = MainStoreWebTestName,
                ClearanceStoreWebTestSubscriptionId = ClearanceStoreWebTestSubscriptionId,
                ClearanceStoreWebTestResourceGroup = ClearanceStoreWebTestResourceGroup,
                ClearanceStoreWebTestName = ClearanceStoreWebTestName,
                StagingDbConnectionString = StagingDbConnectionString,
                ImportABCStores = ImportABCStores,
                ImportHawthorneStores = ImportHawthorneStores,
                SkipOldMattressesImport = SkipOldMattressesImport,
                SkipFillStagingAccessoriesTask = SkipFillStagingAccessoriesTask,
                SkipFillStagingBrandsTask = SkipFillStagingBrandsTask,
                SkipFillStagingPricingTask = SkipFillStagingPricingTask,
                SkipFillStagingProductCategoryMappingsTask = SkipFillStagingProductCategoryMappingsTask,
                SkipFillStagingProductsTask = SkipFillStagingProductsTask,
                SkipFillStagingScandownEndDatesTask = SkipFillStagingScandownEndDatesTask,
                SkipFillStagingWarrantiesTask = SkipFillStagingWarrantiesTask,
                SkipImportProductsTask = SkipImportProductsTask,
                SkipMapCategoriesTask = SkipMapCategoriesTask,
                SkipImportProductCategoryMappingsTask = SkipImportProductCategoryMappingsTask,
                SkipAddHomeDeliveryAttributesTask = SkipAddHomeDeliveryAttributesTask,
                SkipImportMarkdownsTask = SkipImportMarkdownsTask,
                SkipImportRelatedProductsTask = SkipImportRelatedProductsTask,
                SkipImportWarrantiesTask = SkipImportWarrantiesTask,
                SkipUnmapNonstockClearanceTask = SkipUnmapNonstockClearanceTask,
                SkipMapCategoryStoresTask = SkipMapCategoryStoresTask,
                SkipImportDocumentsTask = SkipImportDocumentsTask,
                SkipImportIsamSpecsTask = SkipImportIsamSpecsTask,
                SkipImportFeaturedProductsTask = SkipImportFeaturedProductsTask,
                SkipImportProductFlagsTask = SkipImportProductFlagsTask,
                SkipImportSotPicturesTask = SkipImportSotPicturesTask,
                SkipImportLocalPicturesTask = SkipImportLocalPicturesTask,
                SkipCleanDuplicateImagesTask = SkipCleanDuplicateImagesTask,
                SkipSliExportTask = SkipSliExportTask
            };
        }

        public static ImportSettings CreateDefault()
        {
            return new ImportSettings()
            {
                LastPictureUpdate = DateTime.MinValue
            };
        }

        public bool AreUptimeTestsActive
        {
            get
            {
                return !string.IsNullOrWhiteSpace(MainStoreWebTestSubscriptionId) && !string.IsNullOrWhiteSpace(MainStoreWebTestSubscriptionId) && !string.IsNullOrWhiteSpace(MainStoreWebTestSubscriptionId) &&
                       !string.IsNullOrWhiteSpace(ClearanceStoreWebTestSubscriptionId) && !string.IsNullOrWhiteSpace(ClearanceStoreWebTestResourceGroup) && !string.IsNullOrWhiteSpace(ClearanceStoreWebTestName);
            }
        }

        public string GetSiteOnTimeXmlPath()
        {
            return GetPath("SiteOnTime.xml", "SiteOnTime Xml Path");
        }

        public DirectoryInfo GetPromoPdfDirectory()
        {
            return GetDirectory("promotion_forms/", "Promo PDF directory");
        }

        public DirectoryInfo GetRebatePdfDirectory()
        {
            return GetDirectory("rebate_images/", "Rebate PDF directory");
        }

        public string GetCategoryMappingFile()
        {
            return GetPath("Resources/Mappings to Nop Categories.xlsx", "Category mapping file");
        }

        public string GetFeaturedProductsFile()
        {
            return GetPath("Resources/Featured Products.xlsx", "Featured products file");
        }

        public DirectoryInfo GetLocalPicturesDirectory()
        {
            return GetDirectory("product_images/", "Local pictures directory");
        }

        public string GetEnergyGuidePdfPath()
        {
            return GetPath("wwwroot/energy_guides/", "Energy guide PDF path");
        }

        public string GetSpecificationPdfPath()
        {
            return GetPath("wwwroot/product_specs/", "Specification PDF path");
        }

        public SqlConnection GetStagingDbConnection()
        {
            if (string.IsNullOrWhiteSpace(StagingDbConnectionString))
            {
                throw new ConfigurationErrorsException("Staging DB connection string is not set, please set in AbcSync configuration.");
            }

            return new SqlConnection(StagingDbConnectionString);
        }

        private DirectoryInfo GetDirectory(string path, string description)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ConfigurationErrorsException($"{description} not provided, please set in AbcSync configuration.");
            }

            var directory = Path.Combine(CoreUtilities.WebRootPath(), path);
            if (!Directory.Exists(directory))
            {
                throw new ConfigurationErrorsException($"{description} could not be found at " + directory);
            }

            return new DirectoryInfo(directory);
        }

        private string GetPath(string path, string description)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ConfigurationErrorsException($"{description} not provided, please set in AbcSync configuration.");
            }

            var fullPath = Path.Combine(CoreUtilities.AppPath(), path);
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                throw new ConfigurationErrorsException($"{description} could not be found at " + fullPath);
            }

            return fullPath;
        }
    }
}