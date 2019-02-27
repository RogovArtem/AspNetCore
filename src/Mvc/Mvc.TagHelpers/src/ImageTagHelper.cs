// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="ITagHelper"/> implementation targeting &lt;img&gt; elements that supports file versioning.
    /// </summary>
    /// <remarks>
    /// The tag helper won't process for cases with just the 'src' attribute.
    /// </remarks>
    [HtmlTargetElement(
        "img",
        Attributes = AppendVersionAttributeName + "," + SrcAttributeName,
        TagStructure = TagStructure.WithoutEndTag)]
    public class ImageTagHelper : UrlResolutionTagHelper
    {
        private const string AppendVersionAttributeName = "asp-append-version";
        private const string SrcAttributeName = "src";

        /// <summary>
        /// Creates a new <see cref="ImageTagHelper"/>.
        /// </summary>
        /// <param name="hostingEnvironment">The <see cref="IHostingEnvironment"/>.</param>
        /// <param name="cacheProvider">The <see cref="TagHelperMemoryCacheProvider"/>.</param>
        /// <param name="fileVersionProvider">The <see cref="IFileVersionProvider"/>.</param>
        /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/> to use.</param>
        /// <param name="urlHelperFactory">The <see cref="IUrlHelperFactory"/>.</param>
        // Decorated with ActivatorUtilitiesConstructor since we want to influence tag helper activation
        // to use this constructor in the default case.
        public ImageTagHelper(
            IWebHostEnvironment hostingEnvironment,
            TagHelperMemoryCacheProvider cacheProvider,
            IFileVersionProvider fileVersionProvider,
            HtmlEncoder htmlEncoder,
            IUrlHelperFactory urlHelperFactory)
            : base(urlHelperFactory, htmlEncoder)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            HostingEnvironment = hostingEnvironment;
            Cache = cacheProvider.Cache;
#pragma warning restore CS0618 // Type or member is obsolete
            FileVersionProvider = fileVersionProvider;
        }

        /// <inheritdoc />
        public override int Order => -1000;

        /// <summary>
        /// Source of the image.
        /// </summary>
        /// <remarks>
        /// Passed through to the generated HTML in all cases.
        /// </remarks>
        [HtmlAttributeName(SrcAttributeName)]
        public string Src { get; set; }

        /// <summary>
        /// Value indicating if file version should be appended to the src urls.
        /// </summary>
        /// <remarks>
        /// If <c>true</c> then a query string "v" with the encoded content of the file is added.
        /// </remarks>
        [HtmlAttributeName(AppendVersionAttributeName)]
        public bool AppendVersion { get; set; }

        /// <summary>
        /// The <see cref="IWebHostEnvironment"/> for the application.
        /// </summary>
        [Obsolete("The " + nameof(ImageTagHelper) + " no longer requires the hosting environment.")]
        protected internal IWebHostEnvironment HostingEnvironment { get; }

        /// <summary>
        /// The cache used to store globbed urls.
        /// </summary>
        [Obsolete("The " + nameof(ImageTagHelper) + " no longer requires a memory cache.")]
        protected internal IMemoryCache Cache { get; }

        internal IFileVersionProvider FileVersionProvider { get; private set; }

        /// <inheritdoc />
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            output.CopyHtmlAttribute(SrcAttributeName, context);
            ProcessUrlAttribute(SrcAttributeName, output);

            if (AppendVersion)
            {
                EnsureFileVersionProvider();

                // Retrieve the TagHelperOutput variation of the "src" attribute in case other TagHelpers in the
                // pipeline have touched the value. If the value is already encoded this ImageTagHelper may
                // not function properly.
                Src = output.Attributes[SrcAttributeName].Value as string;

                output.Attributes.SetAttribute(SrcAttributeName, FileVersionProvider.AddFileVersionToPath(ViewContext.HttpContext.Request.PathBase, Src));
            }
        }

        private void EnsureFileVersionProvider()
        {
            if (FileVersionProvider == null)
            {
                FileVersionProvider = ViewContext.HttpContext.RequestServices.GetRequiredService<IFileVersionProvider>();
            }
        }
    }
}