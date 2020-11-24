using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Core.Domain.Topics;
using Nop.Services.Topics;
using Nop.Services.Seo;
using System.Collections.Generic;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using System.Linq;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Services.Catalog;
using System.Text;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Core.Domain.Media;
using Nop.Services.Media;
using Nop.Services.Logging;
using Nop.Plugin.Misc.AbcSync;
using Nop.Plugin.Widgets.AbcPromos;

namespace Nop.Plugin.Widgets.AbcPromos.Tasks.LegacyTasks
{
	public class GenerateRebatePromoPageTask : IScheduleTask
	{
		private readonly IRepository<Topic> _topicRepository;
		private readonly IRepository<Product> _productRepository;
		private readonly IRepository<ProductAbcDescription> _productAbcDescriptionRepository;
		private readonly IRepository<ProductManufacturer> _productManufacturerRepository;

		private readonly IAbcPromoService _abcPromoService;
		private readonly IManufacturerService _manufacturerService;
		private readonly IPictureService _pictureService;
		private readonly IProductService _productService;
		private readonly ITopicService _topicService;
		private readonly IUrlRecordService _urlRecordService;

		private readonly ILogger _logger;

		private readonly MediaSettings _mediaSettings;
		private readonly ImportSettings _importSettings;
		private readonly AbcPromosSettings _settings;

		public GenerateRebatePromoPageTask(
			IRepository<Topic> topicRepository,
			IRepository<Product> productRepository,
			IRepository<ProductAbcDescription> productAbcDescriptionRepository,
			IRepository<ProductManufacturer> productManufacturerRepository,
			IAbcPromoService abcPromoService,
			IManufacturerService manufacturerService,
			IPictureService pictureService,
			IProductService productService,
			ITopicService topicService,
			IUrlRecordService urlRecordService,
			ILogger logger, 
			MediaSettings mediaSettings,
			ImportSettings importSettings,
			AbcPromosSettings settings
		)
		{
			_topicRepository = topicRepository;
			_productRepository = productRepository;
			_productAbcDescriptionRepository = productAbcDescriptionRepository;
			_productManufacturerRepository = productManufacturerRepository;
			_abcPromoService = abcPromoService;
			_manufacturerService = manufacturerService;
			_pictureService = pictureService;
			_productService = productService;
			_topicService = topicService;
			_urlRecordService = urlRecordService;
			_logger = logger;
			_mediaSettings = mediaSettings;
			_importSettings = importSettings;
			_settings = settings;
		}

		public void Execute()
		{
            this.LogStart();
			
			var rebatePromoTopicName = "Rebates and Promos";
			var topic = _topicService.GetTopicBySystemName(rebatePromoTopicName);
			if (topic == null)
			{
				topic = new Topic
				{
					SystemName = rebatePromoTopicName,
					IncludeInFooterColumn1 = true,
					LimitedToStores = false,
					Title = rebatePromoTopicName,
					TopicTemplateId = 1,
					Published = true
				};
				_topicService.InsertTopic(topic);
				_urlRecordService.SaveSlug(topic, _urlRecordService.ValidateSeName(topic, rebatePromoTopicName, topic.Title, true), 0);
			}

			topic.Body = GetRebatePromoHtml(topic);
			_topicService.UpdateTopic(topic);

            this.LogEnd();
		}

		private string GetRebatePromoHtml(Topic rootTopic)
		{
			var html = $"<h2 class=\"abc-rebate-promo-title\">" +
						"Promos</h2><div class=\"abc-container abc-promo-container\">";

			var promos = _settings.IncludeExpiredPromosOnRebatesPromosPage ? 
							_abcPromoService.GetActivePromos().Union(_abcPromoService.GetExpiredPromos()) :
							_abcPromoService.GetActivePromos();
			foreach (var promo in promos)
			{
				var publishedPromoProducts = _abcPromoService.GetPublishedProductsByPromoId(promo.Id);
				if (!publishedPromoProducts.Any())
				{
					_logger.Warning($"Promo {promo.Name} has no associated published products, skipping display on page.");
					continue;
				}

				var promoDescription = promo.ManufacturerId != null ?
								$"{_manufacturerService.GetManufacturerById(promo.ManufacturerId.Value).Name} - {promo.Description}" :
								promo.Description;
				html += $"<div class=\"abc-item abc-promo-item\"> " +
						$"<a href=\"/promos/{_urlRecordService.GetActiveSlug(promo.Id, "AbcPromo", 0)}\"> " +
						$"{promoDescription}</a><br />" +
						$"Expires {promo.EndDate.ToString("MM-dd-yy")}" +
						"</div>";
			}

			html += "</div>";

			return html;
		}
	}
}