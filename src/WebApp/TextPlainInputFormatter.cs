using Microsoft.AspNetCore.Mvc.Formatters;
using System.Text;

namespace WebApp;

// From: https://github.com/aspnet/Entropy/blob/master/samples/Mvc.Formatters/TextPlainInputFormatter.cs
public class TextPlainInputFormatter : TextInputFormatter
{
    public TextPlainInputFormatter()
    {
        // Accept really anything and read directly from body
        SupportedMediaTypes.Add("text/plain");
        SupportedMediaTypes.Add("application/x-www-form-urlencoded");
        SupportedMediaTypes.Add("application/json");

        SupportedEncodings.Add(UTF8EncodingWithoutBOM);
        SupportedEncodings.Add(UTF16EncodingLittleEndian);
    }

    public override bool CanRead(InputFormatterContext context)
    {
        if (string.IsNullOrEmpty(context.HttpContext.Request.ContentType))
        {
            // Default to this formatter if no content type was specified.
            return CanReadType(context.ModelType);
        }
        return base.CanRead(context);
    }

    protected override bool CanReadType(Type type)
    {
        return type == typeof(string);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
    {
        string data = null;
        if (context.HttpContext.Request.HasFormContentType && context.HttpContext.Request.Form.Count != 0)
        {
            var sb = new StringBuilder();
            foreach (var (key, value) in context.HttpContext.Request.Form)
            {
                if (string.IsNullOrEmpty(value))
                {
                    sb.Append($"{key}");
                }
                else
                {
                    sb.Append($"{key}={value}");
                }
            }
            data = sb.ToString();
        }
        else
        {
            using var streamReader = context.ReaderFactory(context.HttpContext.Request.Body, encoding);
            data = await streamReader.ReadToEndAsync();
        }

        return InputFormatterResult.Success(data);
    }
}
