using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RedditReverseProxy.Middleware
{
    public class CustomResponseModifier
    {
        private readonly RequestDelegate _next;

        public CustomResponseModifier(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var buffer = new MemoryStream();
            var originalBody = context.Response.Body;

            context.Response.Body = buffer;

            await _next(context);

            var contentType = context.Response.ContentType;
            if (contentType != null && contentType.Contains("text/html"))
            {
                buffer.Position = 0;
                var responseBody = await new StreamReader(buffer).ReadToEndAsync();

                if (!string.IsNullOrEmpty(responseBody))
                {
                    var modifiedBody = ModifyHtml(responseBody, context.Request.Host);
                    context.Response.ContentLength = Encoding.UTF8.GetBytes(modifiedBody).Length;
                    await originalBody.WriteAsync(Encoding.UTF8.GetBytes(modifiedBody));
                }
            }
            buffer.Dispose();
        }


        public string ModifyHtml(string html, HostString host)
        {
            var modifiedHtml = AddTrademarkToSixLetterWords(html);
            modifiedHtml = modifiedHtml.Replace("reddit.com", host.ToString());

            return modifiedHtml;
        }

        public static string AddTrademarkToSixLetterWords(string htmlString)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlString);

            HtmlNode bodyNode = doc.DocumentNode.SelectSingleNode("//body");
            if (bodyNode == null)
            {
                return htmlString;
            }
            string pattern = @"\b\w{6}\b";
            Regex regex = new Regex(pattern);

            foreach (HtmlNode node in bodyNode.DescendantsAndSelf())
            {
                if (node.AncestorsAndSelf("script").Any() || node.AncestorsAndSelf("style").Any() || node.AncestorsAndSelf("noscript").Any())
                {
                    continue;
                }

                if (node.NodeType == HtmlNodeType.Text)
                {
                    string originalText = node.InnerText;
                    string modifiedText = regex.Replace(originalText, m => m.Value + "™");
                    node.InnerHtml = modifiedText;
                }
            }

            string modifiedHtmlString = doc.DocumentNode.OuterHtml;

            return modifiedHtmlString;
        }
    }
}