using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ASTDAT.Web.Infrastructure
{
    public class MvcIntModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            try
            {
                var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
                string str = value.AttemptedValue;
                str = str.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                str = str.Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                var pos = str.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                if (pos > 0)
                {
                    str = str.Substring(0, pos);
                }
                if (String.IsNullOrWhiteSpace(str))
                {
                    str = "0";
                }

                int i;
                if (!int.TryParse(str, out i))
                {
                    i = 0;
                }
                return i;
            }
            catch
            {
            }
            return null;
        }
    }

    public class MvcIntNullModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            try
            {
                var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
                string str = value.AttemptedValue;
                if (String.IsNullOrEmpty(str))
                {
                    return null;
                }
                str = str.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                str = str.Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                var pos = str.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                if (pos > 0)
                {
                    str = str.Substring(0, pos);
                }
                if (String.IsNullOrWhiteSpace(str))
                {
                    str = "0";
                }

                int i;
                if (!int.TryParse(str, out i))
                {
                    return null;
                }
                return i;
            }
            catch
            {
            }
            return null;
        }
    }
}