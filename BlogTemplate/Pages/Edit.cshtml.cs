using System;
using System.ComponentModel.DataAnnotations;
using BlogTemplate._1.Models;
using BlogTemplate._1.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace BlogTemplate._1.Pages
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly BlogDataStore _dataStore;
        private readonly SlugGenerator _slugGenerator;
        private readonly ExcerptGenerator _excerptGenerator;

        public EditModel(BlogDataStore dataStore, SlugGenerator slugGenerator, ExcerptGenerator excerptGenerator)
        {
            _dataStore = dataStore;
            _slugGenerator = slugGenerator;
            _excerptGenerator = excerptGenerator;
        }

        [BindProperty]
        public EditedPostModel EditedPost { get; set; }
        public bool hasSlug { get; set; }

        public void OnGet([FromRoute] int id)
        {
            Post post = _dataStore.GetPost(id);

            EditedPost = new EditedPostModel
            {
                Title = post.Title,
                Body = post.Body,
                Excerpt = post.Excerpt,
            };

            hasSlug = !string.IsNullOrEmpty(post.Slug);

            if (post == null)
            {
                RedirectToPage("/Index");
            }

            ViewData["Id"] = post.Id;
        }

        [ValidateAntiForgeryToken]
        public IActionResult OnPostPublish([FromRoute] int id, [FromForm] bool updateSlug)
        {
            Post post = _dataStore.GetPost(id);
            if (ModelState.IsValid)
            {
                bool wasPublic = post.IsPublic;
                post.IsPublic = true;
                if (post.PubDate.Equals(default(DateTimeOffset)))
                {
                    post.PubDate = DateTimeOffset.Now;
                }
                if (!hasSlug)
                {
                    post.Slug = _slugGenerator.CreateSlug(post.Title);
                }
                UpdatePost(post, updateSlug, wasPublic);
            }
            return Redirect($"/Post/{id}/{post.Slug}");
        }

        [ValidateAntiForgeryToken]
        public IActionResult OnPostSaveDraft([FromRoute] int id)
        {
            Post post = _dataStore.GetPost(id);
            if (ModelState.IsValid)
            {
                bool wasPublic = post.IsPublic;
                post.IsPublic = false;
                UpdatePost(post, false, wasPublic);
            }
            return Redirect("/Drafts");
        }
        private void UpdatePost(Post post, [FromForm] bool updateSlug, bool wasPublic)
        {
            post.Title = EditedPost.Title;
            post.Body = EditedPost.Body;
            if (string.IsNullOrEmpty(EditedPost.Excerpt))
            {
                EditedPost.Excerpt = _excerptGenerator.CreateExcerpt(EditedPost.Body);
            }
            post.Excerpt = EditedPost.Excerpt;

            if (updateSlug)
            {
                post.Slug = _slugGenerator.CreateSlug(post.Title);
            }
            _dataStore.UpdatePost(post, wasPublic);
        }

        public class EditedPostModel
        {
            [Required]
            public string Title { get; set; }
            [Required]
            public string Body { get; set; }
            public string Excerpt { get; set; }
        }
    }
}
