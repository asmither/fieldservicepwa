using System.Text.Json;

using ICS.Portal.Data.Enumerations;
using ICS.Portal.Data.Images;
using ICS.Portal.Data.Queries.Models;
using ICS.Portal.Data.Extensions;
using ICS.Mobile.Services;

namespace ICS.Portal.Data.Custom;

public class WorkflowReviewBuilder
{
    private static async Task<Dictionary<string, string>> GetEquipmentImagesBase64(WorkflowEquipment? equip, IWorkflowData hydrator)
    {
        Dictionary<string, string> result = new();

        if (equip is not null)
        {
            if (!string.IsNullOrEmpty(equip.UnitImage1FileName))
            {
                ImageInsertInput unitImage1 = new ImageInsertInput()
                {
                    EquipmentId = equip.Id,
                    TemporaryEquipmentId = equip.TemporaryId,
                    ImageIndex = 1
                };

                unitImage1.ParseContentType(equip.UnitImage1FileName);

                await hydrator.HydrateImageAsync(unitImage1);
                if (unitImage1.Data is not null && unitImage1.Data.Length != 0)
                {
                    result.Add(equip.UnitImage1FileName, unitImage1.Base64Version());
                }
            }

            if (!string.IsNullOrEmpty(equip.UnitImage2FileName))
            {
                ImageInsertInput unitImage2 = new ImageInsertInput()
                {
                    EquipmentId = equip.Id,
                    TemporaryEquipmentId = equip.TemporaryId,
                    ImageIndex = 2
                };

                unitImage2.ParseContentType(equip.UnitImage2FileName);
                await hydrator.HydrateImageAsync(unitImage2);


                if (unitImage2.Data is not null && unitImage2.Data.Length != 0)
                {
                    result.Add(equip.UnitImage2FileName, unitImage2.Base64Version());
                }
            }

            if (!string.IsNullOrEmpty(equip.DataPlateFileName))
            {
                ImageInsertInput dataPlateImage = new ImageInsertInput()
                {
                    EquipmentId = equip.Id,
                    TemporaryEquipmentId = equip.TemporaryId,
                    ImageIndex = 3
                };

                dataPlateImage.ParseContentType(equip.DataPlateFileName);
                await hydrator.HydrateImageAsync(dataPlateImage);
                if (dataPlateImage.Data is not null && dataPlateImage.Data.Length != 0)
                {
                    result.Add(equip.DataPlateFileName, dataPlateImage.Base64Version());
                }
            }
        }

        return result;
    }
    public static async Task<List<WorkflowReviewItem>> BuildReviewItems(SettingsService settings, IWorkflowData hydrator, List<WorkflowResultDetailResult.WorkflowStepValue> allValues, List<WorkflowResultDetailResult.WorkflowStep> allSteps, List<WorkflowResultDetailResult.WorkflowStepOption> allOptions, DispatchDetailResult? dispatch, LookupsResult lookups, bool shareExternalOnly = true)
    {
        List<WorkflowReviewItem> result = new();
        List<WorkflowEquipment> equipmentList = new();

        if (dispatch is not null)
        {
            equipmentList = WorkflowEquipmentBuilder.GenerateEquipmentList(dispatch, lookups);
        }

        foreach (var workflowStepResultValue in allValues!)
        {
            if (string.IsNullOrEmpty(workflowStepResultValue.Value) && string.IsNullOrEmpty(workflowStepResultValue.Note) && string.IsNullOrEmpty(workflowStepResultValue.ImageFileName) && string.IsNullOrEmpty(workflowStepResultValue.JsonPayload))
            {
                continue;
            }

            var step = allSteps?.FirstOrDefault(s => s.WorkflowStepResultId == workflowStepResultValue.WorkflowStepResultId);

            // Walk all the steps (Can filter out the share external here, or in the renderer - think renderer is best)
            if (step is not null && (shareExternalOnly.Equals(false) || step.ShareExternally.Equals(shareExternalOnly)))
            {
                ImageInsertInput? imageInsertInput = null;

                string? fileName = string.Empty;
                if (step.WorkflowDataTypeId == (int)WorkflowDataTypes.Signature)
                {
                    fileName = workflowStepResultValue.Value;
                }
                else
                {
                    fileName = workflowStepResultValue.ImageFileName;
                }

                // LoopIndex fix for V2 Complexity Workflows
                if (workflowStepResultValue.WorkflowVersion.GetValueOrDefault(0) == 2)
                {
                    // This is a V2 complexity workflow - 
                    // Set all loop 0 items to be loop 1 so it flattens right and order is ok
                    //if (workflowStepResultValue.LoopIndex == 0)
                    //{
                    //    workflowStepResultValue.LoopIndex = 1;
                    //}

                }

                if (!string.IsNullOrEmpty(fileName))
                {
                    imageInsertInput = new()
                    {
                        WorkflowResultId = workflowStepResultValue.WorkflowResultId,
                        WorkflowStepResultId = workflowStepResultValue.WorkflowStepResultId,
                        WorkflowStepLoopIndex = workflowStepResultValue.LoopIndex,
                        WorkflowStepResultValueId = workflowStepResultValue.WorkflowStepResultValueId,
                        ImageIndex = GetLabelIndex(fileName, 0),
                        Data = null,
                        ImageLabel = step.ImageLabel
                    };

                    imageInsertInput.ParseContentType(fileName);

                    await hydrator.HydrateImageAsync(imageInsertInput);
                    if (imageInsertInput.Data is null || imageInsertInput.Data.Length == 0)
                    {
                        imageInsertInput = null;
                    }
                }

                if (string.IsNullOrEmpty(step.Description) || step.Description.Length < 2) step.Description = null;
                if (string.IsNullOrEmpty(step.HelpText)) step.HelpText = string.Empty;
                if (string.IsNullOrEmpty(step.ImageLabel) || step.ImageLabel.Length < 1) step.ImageLabel = null;

                bool isConsumable = false;
                bool isItem = false;
                bool isCheckList = false;

                if (step.AttributeId.HasValue)
                {
                    var attr = lookups.AttributeResult?.FirstOrDefault(a => a.Id == step.AttributeId.Value);
                    if (attr is not null)
                    {
                        isConsumable = attr.IsConsumable;
                        isItem = attr.IsItem;
                        isCheckList = attr.IsChecklist;
                    }

                }
                WorkflowReviewItem workflowReviewItem = new WorkflowReviewItem()
                {
                    BaseImagesURL = settings.ImageBaseUrl,
                    DataType = step.WorkflowDataTypeId,
                    Prompt = step.Prompt,
                    Value = workflowStepResultValue.Value,
                    LoopIndex = workflowStepResultValue.LoopIndex,
                    LoopPath = workflowStepResultValue.LoopPath,
                    WorkflowStepResultValueId = workflowStepResultValue.WorkflowStepResultValueId,
                    WorkflowVersion = workflowStepResultValue.WorkflowVersion.GetValueOrDefault(0),
                    Note = workflowStepResultValue.Note,
                    ImageFileName = workflowStepResultValue.ImageFileName,
                    NoteLabel = step.NoteLabel,
                    ImageLabel = step.ImageLabel,
                    ImageData = imageInsertInput,
                    IsConsumable = isConsumable,
                    IsChecklist = isCheckList,
                    IsItem = isItem
                };


                if ((WorkflowDataTypes)step.WorkflowDataTypeId == WorkflowDataTypes.Time)
                {
                    workflowReviewItem.DisplayValue = workflowReviewItem.Value.ConvertToLocalWithTimeZone();
                }
                else if ((WorkflowDataTypes)step.WorkflowDataTypeId == WorkflowDataTypes.Choice)
                {
                    var option = allOptions.FirstOrDefault(o => o.WorkflowStepId.Equals(step.WorkflowStepId) && o.Value.Equals(workflowStepResultValue.Value));
                    if (option is not null)
                    {
                        workflowReviewItem.DisplayValue = option.Prompt;
                        workflowReviewItem.ImageLabel = option.ImageLabel;
                        workflowReviewItem.NoteLabel = option.NoteLabel;
                    }
                }
                else if ((WorkflowDataTypes)step.WorkflowDataTypeId == WorkflowDataTypes.ParallelSplit)
                {
                    //var option = allOptions.FirstOrDefault(o => o.WorkflowStepId == step.WorkflowStepId);

                    var optionList = workflowReviewItem?.Value?.Split(',');
                    if (optionList is not null && optionList.Length > 0 && allSteps is not null)
                    {
                        string NewPrompt = string.Empty;
                        foreach (var option in optionList)
                        {
                            int GrabStepId = 0;
                            if (int.TryParse(option, out GrabStepId))
                            {
                                var FoundStepRef = allSteps.Find(x => x.WorkflowStepResultId.Equals(GrabStepId));
                                if (FoundStepRef is not null)
                                {
                                    if (string.IsNullOrWhiteSpace(NewPrompt))
                                        NewPrompt = FoundStepRef.Prompt;
                                    else
                                    {
                                        NewPrompt = $"{NewPrompt}\r\n{FoundStepRef.Prompt}";
                                    }

                                }
                            }

                        }
                        workflowReviewItem.DisplayValue = NewPrompt;
                    }


                }
                else if ((WorkflowDataTypes)step.WorkflowDataTypeId == WorkflowDataTypes.Split || (WorkflowDataTypes)step.WorkflowDataTypeId == WorkflowDataTypes.AutoSwitch)
                {
                    //var option = allOptions.FirstOrDefault(o => o.WorkflowStepId == step.WorkflowStepId);
                    var option = allOptions.FirstOrDefault(o => o.WorkflowStepId == step.WorkflowStepId && o.Value == workflowReviewItem.Value);
                    if (option is not null)
                    {
                        workflowReviewItem.Value = option.Value;
                        workflowReviewItem.DisplayValue = option.Prompt;
                        workflowReviewItem.ImageLabel = option.ImageLabel;
                        workflowReviewItem.NoteLabel = option.NoteLabel;

                    }
                }
                else if ((WorkflowDataTypes)step.WorkflowDataTypeId == WorkflowDataTypes.Boolean)
                {
                    if (bool.TryParse(workflowReviewItem.Value, out bool parsedValue))
                    {
                        if (!string.IsNullOrEmpty(step.WorkflowDataTypeDetail))
                        {
                            Dictionary<string, string> values = JsonSerializer.Deserialize<Dictionary<string, string>>(step.WorkflowDataTypeDetail)!;
                            WorkflowDataTypeDetail detail = new WorkflowDataTypeDetail(values);

                            if (parsedValue == true)
                            {
                                workflowReviewItem.DisplayValue = detail.TruePrompt;
                                workflowReviewItem.ImageLabel = detail.TrueImageLabel;
                                workflowReviewItem.NoteLabel = detail.TrueNoteLabel;

                            }
                            else
                            {
                                workflowReviewItem.DisplayValue = detail.FalsePrompt;
                                workflowReviewItem.ImageLabel = detail.FalseImageLabel;
                                workflowReviewItem.NoteLabel = detail.FalseNoteLabel;
                            }
                        }
                    }
                }
                else if ((WorkflowDataTypes)step.WorkflowDataTypeId == WorkflowDataTypes.Equipment)
                {
                    if (string.IsNullOrEmpty(workflowStepResultValue.JsonPayload))
                    {
                        workflowReviewItem.SelectedEquipment = EquipmentFromList(equipmentList, workflowReviewItem.Value);
                    }
                    else
                    {
                        try
                        {
                            var jsonList = JsonSerializer.Deserialize<List<WorkflowEquipment>>(workflowStepResultValue.JsonPayload);
                            if (jsonList is not null)
                            {
                                workflowReviewItem.SelectedEquipment = EquipmentFromList(jsonList, workflowReviewItem.Value);
                            }
                        }
                        catch
                        {
                            workflowReviewItem.SelectedEquipment = null;
                        }
                        if (workflowReviewItem.SelectedEquipment is null && !string.IsNullOrEmpty(workflowReviewItem.Value))
                            workflowReviewItem.SelectedEquipment = EquipmentFromList(equipmentList, workflowReviewItem.Value);

                    }
                    if (workflowReviewItem.SelectedEquipment is not null)
                    {
                        workflowReviewItem.EquipmentImages = await GetEquipmentImagesBase64(workflowReviewItem.SelectedEquipment, hydrator);
                    }

                    //TODO: Create a modified equipment list to show at the bottom of the review
                    // This list would be at the root level;

                }
                else if ((WorkflowDataTypes)step.WorkflowDataTypeId == WorkflowDataTypes.Photos || (WorkflowDataTypes)step.WorkflowDataTypeId == WorkflowDataTypes.Files)
                {
                    if (!string.IsNullOrEmpty(workflowStepResultValue.Value))
                    {
                        workflowReviewItem.ImageNames = workflowStepResultValue.Value.Split(",", StringSplitOptions.None);
                        workflowReviewItem.ImageLabels = new string[workflowReviewItem.ImageNames.Length];
                        workflowReviewItem.ImageDatas = new ImageInsertInput[workflowReviewItem.ImageNames.Length];

                        string[]? imageFileLabels = new string[workflowReviewItem.ImageDatas.Length];
                        Dictionary<string, string> detail;

                        if (!string.IsNullOrEmpty(step.WorkflowDataTypeDetail))
                        {
                            detail = JsonSerializer.Deserialize<Dictionary<string, string>>(step.WorkflowDataTypeDetail)!;
                            if (detail.ContainsKey("Labels"))
                            {
                                imageFileLabels = detail["Labels"].Split("~", StringSplitOptions.None);
                            }
                        }

                        for (int idx = 0; idx < workflowReviewItem.ImageNames.Length; idx++)
                        {

                            int tmpIdx = GetLabelIndex(workflowReviewItem.ImageNames[idx], idx);
                            string tmpLabel = "Photo Taken";

                            if (imageFileLabels is { } labels && tmpIdx >= 0 && tmpIdx < labels.Length)
                                tmpLabel = labels[tmpIdx] ?? step.ImageLabel ?? "";
                            else
                                tmpLabel = step.ImageLabel ?? "";

                            ImageInsertInput imageInsertInputItem = new ImageInsertInput()
                            {
                                WorkflowResultId = workflowStepResultValue.WorkflowResultId,
                                WorkflowStepResultId = workflowStepResultValue.WorkflowStepResultId,
                                WorkflowStepLoopIndex = workflowStepResultValue.LoopIndex,
                                WorkflowStepResultValueId = workflowStepResultValue.WorkflowStepResultValueId,
                                ImageIndex = tmpIdx,
                                Data = null,
                                ImageLabel = tmpLabel
                            };

                            imageInsertInputItem.ParseContentType(workflowReviewItem.ImageNames[idx]);


                            // make sure we have filename
                            if (!string.IsNullOrWhiteSpace(workflowReviewItem.ImageNames[idx]) &&
                                workflowReviewItem.ImageNames[idx] != imageInsertInputItem.GeneratedFileName)
                            {
                                imageInsertInputItem.GeneratedFileName = workflowReviewItem.ImageNames[idx];
                            }

                            await hydrator.HydrateImageAsync(imageInsertInputItem);
                            if (imageInsertInputItem!.Data is not null && imageInsertInputItem.Data.Length != 0)
                            {
                                workflowReviewItem.ImageDatas[idx] = imageInsertInputItem;
                            }
                            // else { } // todo - hydrate image
                        }


                        // get labels
                        if (!string.IsNullOrEmpty(step.WorkflowDataTypeDetail))
                        {

                            for (int idx = 0; idx < workflowReviewItem.ImageNames.Length; idx++)
                            {
                                if (imageFileLabels.Length > idx)
                                {
                                    if (workflowReviewItem.ImageDatas is not null && workflowReviewItem.ImageDatas[idx] is not null)
                                    {
                                        workflowReviewItem.ImageLabels[idx] = workflowReviewItem.ImageDatas[idx].ImageLabel ?? step.ImageLabel;
                                    }
                                }
                            }

                        }

                    }
                }
                else if ((WorkflowDataTypes)step.WorkflowDataTypeId == WorkflowDataTypes.Calculator)
                {
                    Dictionary<string, string> values = JsonSerializer.Deserialize<Dictionary<string, string>>(step.WorkflowDataTypeDetail)!;
                    CalculationItems calculationItems = new CalculationItems(values["CalculationItems"]);

                    if (!string.IsNullOrEmpty(calculationItems.Presentation))
                    {
                        workflowReviewItem.Value = calculationItems.Presentation.Replace("[CALCULATION]", workflowReviewItem.Value);
                    }
                }
                if (workflowReviewItem.HasData())
                {
                    result.Add(workflowReviewItem);
                }
            }
        }

        return result;
    }


    private static int GetLabelIndex(string? fileName, int defaultIndex = 0)
    {
        if (string.IsNullOrEmpty(fileName))
            return defaultIndex;

        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        int lastDash = nameWithoutExt.LastIndexOf('-');

        if (lastDash < 0 || lastDash == nameWithoutExt.Length - 1)
            return defaultIndex;

        return int.TryParse(nameWithoutExt[(lastDash + 1)..], out int idx)
            ? idx
            : defaultIndex;
    }


    private static WorkflowEquipment? EquipmentFromList(List<WorkflowEquipment> equipmentList, string? value)
    {
        WorkflowEquipment? result = null;

        if (int.TryParse(value, out int intResult))
        {
            result = equipmentList!.FirstOrDefault(e => e.Id == intResult);
        }
        else if (Guid.TryParse(value, out Guid guidResult))
        {
            result = equipmentList!.FirstOrDefault(e => e.TemporaryId == guidResult);
        }

        return result;

    }
}
