using Orchard.DynamicForms.Services;
using Orchard.DynamicForms.Services.Models;
using Orchard.Environment.Extensions;
using River.DynamicForms.Elements;
using System.Collections.Generic;
using System.IO;
using Orchard.Layouts.Elements;
using Orchard.Layouts.Framework.Elements;

namespace River.DynamicForms.Handlers
{
    [OrchardFeature("River.DynamicForms.Elements.FileField")]
    public class FormFileSubmissionCoordinator : FormEventHandlerBase
    {
        public override void Submitted(FormSubmittedEventContext context)
        {
            var allFormElements = new List<Element>();
            HarvestFormElements(context.Form.Elements, allFormElements);

            foreach (var element in allFormElements)
            {
                var fileFieldElement = element as FileField;

                if (fileFieldElement == null)
                    continue;

                var postedFileValue = context.ValueProvider.GetValue(fileFieldElement.Name);

                if (postedFileValue == null)
                    return;

                var postedFile = ((System.Web.HttpPostedFileBase[])(postedFileValue.RawValue))[0];

                var path = Path.Combine(fileFieldElement.FilePath, Path.GetFileName(postedFile.FileName));

                if (fileFieldElement.GenerateUnique)
                {
                    int count = 1;
                    var pathPattern = Path.Combine(fileFieldElement.FilePath, string.Format("{0}_{{0}}{1}", Path.GetFileNameWithoutExtension(postedFile.FileName), Path.GetExtension(postedFile.FileName)));
                    while (File.Exists(string.Format(pathPattern, count)))
                    {
                        count++;
                    }

                    path = string.Format(pathPattern, count);
                }

                context.Values[fileFieldElement.Name + ":size"] = postedFile.ContentLength.ToString();
                context.Values[fileFieldElement.Name] = path;
                fileFieldElement.PostedValue = path;
            }
        }

        public override void Validated(FormValidatedEventContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var allFormElements = new List<Element>();
                HarvestFormElements(context.Form.Elements, allFormElements);

                //Clean up on validation fail
                foreach (var element in allFormElements)
                {
                    if (element is FileField)
                    {
                        var fileFieldElement = (FileField)element;

                        var path = context.Values[fileFieldElement.Name];

                        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                            File.Delete(path);
                    }
                }
            }
        }

        public override void Validating(FormValidatingEventContext context)
        {
            var allFormElements = new List<Element>();
            HarvestFormElements(context.Form.Elements, allFormElements);

            foreach (var element in allFormElements)
            {
                var fileFieldElement = element as FileField;

                if (fileFieldElement == null)
                    continue;

                try
                {
                    var path = context.Values[fileFieldElement.Name];

                    if (string.IsNullOrWhiteSpace(path))
                        return;

                    var postedFileValue = context.ValueProvider.GetValue(fileFieldElement.Name);

                    if (postedFileValue == null)
                        return;

                    var postedFile = ((System.Web.HttpPostedFileBase[])(postedFileValue.RawValue))[0];

                    using (var fileStream = File.Create(path))
                    {
                        postedFile.InputStream.Seek(0, SeekOrigin.Begin);
                        postedFile.InputStream.CopyTo(fileStream);
                    }
                }
                catch
                {
                    context.ModelState.AddModelError(fileFieldElement.Name, "Error Saving File");
                }
            }
        }

        private void HarvestFormElements(IList<Element> parentElements, List<Element> result)
        {
            result.AddRange(parentElements);

            foreach (var element in parentElements)
            {
                var containerElement = element as Container;
                if (containerElement != null)
                {
                    // Containers contain other elements, so loop these too.
                    HarvestFormElements(containerElement.Elements, result);
                }
            }
        }
    }
}