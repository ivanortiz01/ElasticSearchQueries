using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using ElasticSearchQueries.Controllers;
using System;
using System.Linq;
using System.Security.Claims;
using WilderMinds.MetaWeblog;
using System.Threading.Tasks;

namespace ElasticSearchQueries
{
    public class MetaWeblogService : IMetaWeblogProvider
    {
        private IBlogService _blog;
        private IConfiguration _config;
        private IHttpContextAccessor _context;

        public MetaWeblogService(IBlogService blog, IConfiguration config, IHttpContextAccessor context)
        {
            _blog = blog;
            _config = config;
            _context = context;
        }

        public string AddPost(string blogid, string username, string password, WilderMinds.MetaWeblog.Post post, bool publish)
        {
            ValidateUser(username, password);

            var newPost = new Models.Post
            {
                Title = post.title,
                Slug = !string.IsNullOrWhiteSpace(post.wp_slug) ? post.wp_slug : Models.Post.CreateSlug(post.title),
                Content = post.description,
                IsPublished = publish,
                Categories = post.categories
            };

            if (post.dateCreated != DateTime.MinValue)
            {
                newPost.PubDate = post.dateCreated;
            }

            _blog.SavePost(newPost).GetAwaiter().GetResult();

            return newPost.ID;
        }

        public bool DeletePost(string key, string postid, string username, string password, bool publish)
        {
            ValidateUser(username, password);

            var post = _blog.GetPostById(postid).GetAwaiter().GetResult();

            if (post != null)
            {
                _blog.DeletePost(post).GetAwaiter().GetResult();
                return true;
            }

            return false;
        }

        public bool EditPost(string postid, string username, string password, WilderMinds.MetaWeblog.Post post, bool publish)
        {
            ValidateUser(username, password);

            var existing = _blog.GetPostById(postid).GetAwaiter().GetResult();

            if (existing != null)
            {
                existing.Title = post.title;
                existing.Slug = post.wp_slug;
                existing.Content = post.description;
                existing.IsPublished = publish;
                existing.Categories = post.categories;

                if (post.dateCreated != DateTime.MinValue)
                {
                    existing.PubDate = post.dateCreated;
                }

                _blog.SavePost(existing).GetAwaiter().GetResult();

                return true;
            }

            return false;
        }

        public CategoryInfo[] GetCategories(string blogid, string username, string password)
        {
            ValidateUser(username, password);

            return _blog.GetCategories().GetAwaiter().GetResult()
                           .Select(cat =>
                               new CategoryInfo
                               {
                                   categoryid = cat,
                                   title = cat
                               })
                           .ToArray();
        }

        public WilderMinds.MetaWeblog.Post GetPost(string postid, string username, string password)
        {
            ValidateUser(username, password);

            var post = _blog.GetPostById(postid).GetAwaiter().GetResult();

            if (post != null)
            {
                return ToMetaWebLogPost(post);
            }

            return null;
        }

        public WilderMinds.MetaWeblog.Post[] GetRecentPosts(string blogid, string username, string password, int numberOfPosts)
        {
            ValidateUser(username, password);

            return _blog.GetPosts(numberOfPosts).GetAwaiter().GetResult().Select(p => ToMetaWebLogPost(p)).ToArray();
        }

        public BlogInfo[] GetUsersBlogs(string key, string username, string password)
        {
            ValidateUser(username, password);

            var request = _context.HttpContext.Request;
            string url = request.Scheme + "://" + request.Host;

            return new[] { new BlogInfo {
                blogid ="1",
                blogName = _config["blog:name"],
                url = url
            }};
        }

        public MediaObjectInfo NewMediaObject(string blogid, string username, string password, MediaObject mediaObject)
        {
            ValidateUser(username, password);
            byte[] bytes = Convert.FromBase64String(mediaObject.bits);
            string path = _blog.SaveFile(bytes, mediaObject.name).GetAwaiter().GetResult();

            return new MediaObjectInfo { url = path };
        }

        public UserInfo GetUserInfo(string key, string username, string password)
        {
            ValidateUser(username, password);
            throw new NotImplementedException();
        }

        public int AddCategory(string key, string username, string password, NewCategory category)
        {
            ValidateUser(username, password);
            throw new NotImplementedException();
        }

        private void ValidateUser(string username, string password)
        {
            if (username != _config["user:username"] || !AccountController.VerifyHashedPassword(password, _config))
            {
                throw new MetaWeblogException("Unauthorized");
            }

            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, _config["user:username"]));

            _context.HttpContext.User = new ClaimsPrincipal(identity);
        }

        private WilderMinds.MetaWeblog.Post ToMetaWebLogPost(Models.Post post)
        {
            var request = _context.HttpContext.Request;
            string url = request.Scheme + "://" + request.Host;

            return new WilderMinds.MetaWeblog.Post
            {
                postid = post.ID,
                title = post.Title,
                wp_slug = post.Slug,
                permalink = url + post.GetLink(),
                dateCreated = post.PubDate,
                description = post.Content,
                categories = post.Categories.ToArray()
            };
        }

        Task<UserInfo> IMetaWeblogProvider.GetUserInfoAsync(string key, string username, string password)
        {
            throw new NotImplementedException();
        }

        Task<BlogInfo[]> IMetaWeblogProvider.GetUsersBlogsAsync(string key, string username, string password)
        {
            throw new NotImplementedException();
        }

        Task<Post> IMetaWeblogProvider.GetPostAsync(string postid, string username, string password)
        {
            throw new NotImplementedException();
        }

        Task<Post[]> IMetaWeblogProvider.GetRecentPostsAsync(string blogid, string username, string password, int numberOfPosts)
        {
            throw new NotImplementedException();
        }

        Task<string> IMetaWeblogProvider.AddPostAsync(string blogid, string username, string password, Post post, bool publish)
        {
            throw new NotImplementedException();
        }

        Task<bool> IMetaWeblogProvider.DeletePostAsync(string key, string postid, string username, string password, bool publish)
        {
            throw new NotImplementedException();
        }

        Task<bool> IMetaWeblogProvider.EditPostAsync(string postid, string username, string password, Post post, bool publish)
        {
            throw new NotImplementedException();
        }

        Task<CategoryInfo[]> IMetaWeblogProvider.GetCategoriesAsync(string blogid, string username, string password)
        {
            throw new NotImplementedException();
        }

        Task<int> IMetaWeblogProvider.AddCategoryAsync(string key, string username, string password, NewCategory category)
        {
            throw new NotImplementedException();
        }

        Task<Tag[]> IMetaWeblogProvider.GetTagsAsync(string blogid, string username, string password)
        {
            throw new NotImplementedException();
        }

        Task<MediaObjectInfo> IMetaWeblogProvider.NewMediaObjectAsync(string blogid, string username, string password, MediaObject mediaObject)
        {
            throw new NotImplementedException();
        }

        Task<Page> IMetaWeblogProvider.GetPageAsync(string blogid, string pageid, string username, string password)
        {
            throw new NotImplementedException();
        }

        Task<Page[]> IMetaWeblogProvider.GetPagesAsync(string blogid, string username, string password, int numPages)
        {
            throw new NotImplementedException();
        }

        Task<Author[]> IMetaWeblogProvider.GetAuthorsAsync(string blogid, string username, string password)
        {
            throw new NotImplementedException();
        }

        Task<string> IMetaWeblogProvider.AddPageAsync(string blogid, string username, string password, Page page, bool publish)
        {
            throw new NotImplementedException();
        }

        Task<bool> IMetaWeblogProvider.EditPageAsync(string blogid, string pageid, string username, string password, Page page, bool publish)
        {
            throw new NotImplementedException();
        }

        Task<bool> IMetaWeblogProvider.DeletePageAsync(string blogid, string username, string password, string pageid)
        {
            throw new NotImplementedException();
        }
    }
}
