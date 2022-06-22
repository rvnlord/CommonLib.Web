using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using Microsoft.AspNetCore.Components;

namespace CommonLib.Web.Source.Common.Components
{
    public abstract class MyComponentStylesBase : LayoutComponentBase 
    {
        private bool _firstRenderAfterInit;
        private bool _firstParamSetup;
        private readonly SemaphoreSlim _syncClasses = new(1, 1);

        protected Guid _guid { get; set; }
        protected string _renderClasses { get; set; } // this properties prevents async component rendering from throwing if clicking sth fast would change the collection before it is iterated properly within the razor file
      
        protected List<string> _classes { get; } = new();
        
        [Parameter(CaptureUnmatchedValues = true)]
        public Dictionary<string, object> AdditionalAttributes { get; set; } = new();
        
        protected override async Task OnInitializedAsync()
        {
            _guid = Guid.NewGuid();

            //

            _firstRenderAfterInit = true;
            _firstParamSetup = true;

            await Task.CompletedTask;
        }
        
        protected override async Task OnParametersSetAsync()
        {
            SetUserDefinedClasses();

            _firstParamSetup = false;

            await Task.CompletedTask;
        }
        
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_firstRenderAfterInit)
                return;
            _firstRenderAfterInit = false;

            //

            await Task.CompletedTask;
        }
        
        protected void SetUserDefinedClasses()
        {
            _syncClasses.Wait();
            
            var additionalClasses = AdditionalAttributes.VorN("class")?.ToString().NullifyIf(s => s.IsNullOrWhiteSpace())?.Split(" ");
            if (additionalClasses != null)
                _classes.AddRange(additionalClasses.Where(c => !c.IsNullOrWhiteSpace()));
            _renderClasses = _classes.Distinct().JoinAsString(" ");

            _syncClasses.Release();
        }
        
        public override string ToString() => _renderClasses;
    }
}
