﻿// https://sonarsource.atlassian.net/browse/NET-1106
// <auto-generated/>
#pragma warning disable 1591
namespace AspNetCoreGeneratedDocument
{
    #line default
    using TModel = global::System.Object;
    using global::System;
    using global::System.Collections.Generic;
    using global::System.Linq;
    using global::System.Threading.Tasks;
    using global::Microsoft.AspNetCore.Mvc;
    using global::Microsoft.AspNetCore.Mvc.Rendering;
    using global::Microsoft.AspNetCore.Mvc.ViewFeatures;
#nullable restore
#line 1 "C:\Users\sebastien.marichal\Downloads\WebApplication1\WebApplication1\Views\Test.cshtml"
using Microsoft.Extensions.Configuration;

#line default
#line hidden
#nullable disable
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemMetadataAttribute("Identifier", "/Views/Test.cshtml")]
    [global::System.Runtime.CompilerServices.CreateNewOnMetadataUpdateAttribute]
    #nullable restore
    internal sealed class Views_Test : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    #nullable disable
    {
        #pragma warning disable 219
        private void __RazorDirectiveTokenHelpers__() {
        ((global::System.Action)(() => {
#nullable restore
#line 33 "C:\Users\sebastien.marichal\Downloads\WebApplication1\WebApplication1\Views\Test.cshtml" // Original location is line 2 but otherwise it would messed with the line numbers
IConfiguration __typeHelper = default!; // Noncompliant - FP

#line default
#line hidden
#nullable disable
        }
        ))();
        ((global::System.Action)(() => {
#nullable restore
#line 43 "C:\Users\sebastien.marichal\Downloads\WebApplication1\WebApplication1\Views\Test.cshtml" // Original location is line 2 but otherwise it would messed with the line numbers
global::System.Object Configuration = null!; // Noncompliant - FP It should/can not be const
#line default
#line hidden
#nullable disable
        }
        ))();
        }
        #pragma warning restore 219
        #pragma warning disable 0414
        private static object __o = null;
        #pragma warning restore 0414
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#nullable restore
#line 5 "C:\Users\sebastien.marichal\Downloads\WebApplication1\WebApplication1\Views\Test.cshtml"
 if (Configuration != null)
{


#line default
#line hidden
#nullable disable
#nullable restore
#line 7 "C:\Users\sebastien.marichal\Downloads\WebApplication1\WebApplication1\Views\Test.cshtml"
__o = Configuration["Test"];

#line default
#line hidden
#nullable disable
#nullable restore
#line 7 "C:\Users\sebastien.marichal\Downloads\WebApplication1\WebApplication1\Views\Test.cshtml"

}

#line default
#line hidden
#nullable disable
        }
        #pragma warning restore 1998
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public IConfiguration Configuration { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; } = default!;
        #nullable disable
    }
}
#pragma warning restore 1591
