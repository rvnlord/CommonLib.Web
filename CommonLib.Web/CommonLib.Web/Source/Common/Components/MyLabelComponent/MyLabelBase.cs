﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.UtilClasses;
using Microsoft.AspNetCore.Components;

namespace CommonLib.Web.Source.Common.Components.MyLabelComponent
{
    public class MyLabelBase<TProperty> : MyComponentBase
    {
        protected string _displayName { get; set; }
        protected List<string> _descriptionClasses { get; } = new();

        [Parameter]
        public Expression<Func<TProperty>> For { get; set; }

        [Parameter]
        public object Model { get; set; }

        [Parameter]
        public string Value { get; set; }

        [Parameter]
        public HorizontalAlignment Align { get; set; }

        [Parameter] 
        public LabelSizing Sizing { get; set; } = LabelSizing.Default;

        protected override async Task OnParametersSetAsync()
        {
            Model ??= CascadedEditContext?.ParameterValue?.Model; // don't do this: CurrentEditContext ??= new EditContext(Model);
            
            if (For != null && Model != null)
                _displayName = $"{For.GetPropertyDisplayName()}:";
            else if (!Value.IsNullOrWhiteSpace())
                _displayName = Value;

            var customClasses = new List<string>();
            var alignClass = Align switch
            {
                HorizontalAlignment.Left => "my-left",
                HorizontalAlignment.Right => "my-right",
                _ => ""
            };

            customClasses.Add(alignClass);

            if (Sizing == LabelSizing.LineHeight)
                customClasses.Add("my-p-0");

            SetMainCustomAndUserDefinedClasses("my-label", customClasses);

            _descriptionClasses.ReplaceAll("my-label-description");

            SetUserDefinedStyles();

            await Task.CompletedTask;
        }

        protected override async Task OnInitializedAsync() => await Task.CompletedTask;
        protected override async Task OnAfterFirstRenderAsync() => await Task.CompletedTask;
    }

    public enum LabelSizing
    {
        Default,
        LineHeight
    }
}
