using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Atom;
using Microsoft.SyndicationFeed.Rss;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ElasticSearchQueries
{
    public class RobotsController : Controller
    {
        private IBlogService _blog;
        private IOptionsSnapshot<BlogSettings> _settings;

        public RobotsController(IBlogService blog, IOptionsSnapshot<BlogSettings> settings)
        {
            _blog = blog;
            _settings = settings;
        }

        [Route("/robots.txt")]
        [OutputCache(Profile = "default")]
        public string RobotsTxt()
        {
            string host = Request.Scheme + "://" + Request.Host;
            var sb = new StringBuilder();
            sb.AppendLine("User-agent: *");
            sb.AppendLine("Disallow:");
            sb.AppendLine($"sitemap: {host}/sitemap.xml");

            return sb.ToString();
        }

        [Route("/sitemap.xml")]
        public async Task SitemapXml()
        {
            string host = Request.Scheme + "://" + Request.Host;

            Response.ContentType = "application/xml";

            using (var xml = XmlWriter.Create(Response.Body, new XmlWriterSettings { Indent = true }))
            {
                xml.WriteStartDocument();
                xml.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

                var posts = await _blog.GetPosts(int.MaxValue);

                foreach (Models.Post post in posts)
                {
                    var lastMod = new[] { post.PubDate, post.LastModified };

                    xml.WriteStartElement("url");
                    xml.WriteElementString("loc", host + post.GetLink());
                    xml.WriteElementString("lastmod", lastMod.Max().ToString("yyyy-MM-ddThh:mmzzz"));
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
            }
        }

        [Route("/rsd.xml")]
        public void RsdXml()
        {
            string host = Request.Scheme + "://" + Request.Host;

            Response.ContentType = "application/xml";
            Response.Headers["cache-control"] = "no-cache, no-store, must-revalidate";

            using (var xml = XmlWriter.Create(Response.Body, new XmlWriterSettings { Indent = true }))
            {
                xml.WriteStartDocument();
                xml.WriteStartElement("rsd");
                xml.WriteAttributeString("version", "1.0");

                xml.WriteStartElement("service");

                xml.WriteElementString("enginename", "ElasticSearchQueries");
                xml.WriteElementString("enginelink", "http://github.com/madskristensen/ElasticSearchQueries/");
                xml.WriteElementString("homepagelink", host);

                xml.WriteStartElement("apis");
                xml.WriteStartElement("api");
                xml.WriteAttributeString("name", "MetaWeblog");
                xml.WriteAttributeString("preferred", "true");
                xml.WriteAttributeString("apilink", host + "/metaweblog");
                xml.WriteAttributeString("blogid", "1");

                xml.WriteEndElement(); // api
                xml.WriteEndElement(); // apis
                xml.WriteEndElement(); // service
                xml.WriteEndElement(); // rsd
            }
        }

        [Route("/feed/{type}")]
        public async Task Rss(string type)
        {
            Response.ContentType = "application/xml";
            string host = Request.Scheme + "://" + Request.Host;

            using (XmlWriter xmlWriter = XmlWriter.Create(Response.Body, new XmlWriterSettings() { Async = true, Indent = true }))
            {
                var posts = await _blog.GetPosts(10);
                var writer = await GetWriter(type, xmlWriter, posts.Max(p => p.PubDate));

                foreach (Models.Post post in posts)
                {
                    var item = new AtomEntry
                    {
                        Title = post.Title,
                        Description = post.Content,
                        Id = host + post.GetLink(),
                        Published = post.PubDate,
                        LastUpdated = post.LastModified,
                        ContentType = "html",
                    };

                    foreach (string category in post.Categories)
                    {
                        item.AddCategory(new SyndicationCategory(category));
                    }

                    item.AddContributor(new SyndicationPerson(_settings.Value.Owner, "test@example.com"));
                    item.AddLink(new SyndicationLink(new Uri(item.Id)));

                    await writer.Write(item);
                }
            }
        }

        private async Task<ISyndicationFeedWriter> GetWriter(string type, XmlWriter xmlWriter, DateTime updated)
        {
            string host = Request.Scheme + "://" + Request.Host + "/";

            if (type.Equals("rss", StringComparison.OrdinalIgnoreCase))
            {
                var rss = new RssFeedWriter(xmlWriter);
                await rss.WriteTitle(_settings.Value.Name);
                await rss.WriteDescription(_settings.Value.Description);
                await rss.WriteGenerator("ElasticSearchQueries");
                await rss.WriteValue("link", host);
                return rss;
            }

            var atom = new AtomFeedWriter(xmlWriter);
            await atom.WriteTitle(_settings.Value.Name);
            await atom.WriteId(host);
            await atom.WriteSubtitle(_settings.Value.Description);
            await atom.WriteGenerator("ElasticSearchQueries", "https://github.com/madskristensen/ElasticSearchQueries", "1.0");
            await atom.WriteValue("updated", updated.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            return atom;
        }
    }
}
