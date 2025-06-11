using RazorLight;
using WebsiteQuanLyBanHangOnline.Models.ViewModels;

namespace WebsiteQuanLyBanHangOnline.Services.Email
{
    public class EmailTemplateRenderer
    {
        private readonly RazorLightEngine _engine;

        public EmailTemplateRenderer(IWebHostEnvironment env)
        {
            var root = Path.Combine(env.ContentRootPath, "Views", "Shared", "EmailTemplates");

            _engine = new RazorLightEngineBuilder()
                .UseFileSystemProject(root)
                .UseMemoryCachingProvider()
                .SetOperatingAssembly(typeof(EmailOrderViewModel).Assembly)
                .Build();
        }

        public async Task<string> RenderAsync<T>(string templateName, T model)
        {
            return await _engine.CompileRenderAsync(templateName, model);
        }
    }
}
