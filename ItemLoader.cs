using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MelonLoader;

namespace NastyMod
{
    internal class ItemLoader
    {
        public Dictionary<string, List<string>> LoadItems()
        {
            Dictionary<string, List<string>> itemTree = new Dictionary<string, List<string>>();

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "NastyMod.Resources.items.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    MelonLogger.Error("Failed to load items.json! Check if it's embedded correctly.");
                    return itemTree;
                }

                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    // MelonLogger.Msg("Loaded JSON successfully.");

                    // MelonLogger.Msg($"JSON Content: {json}");
                    string[] categories = json.Split(new string[] { "\"category\":" }, StringSplitOptions.RemoveEmptyEntries);
                    // MelonLogger.Msg($"Found {categories.Length} categories");

                    foreach (string cat in categories)
                    {
                        // MelonLogger.Msg($"Processing category: {cat}");

                        if (!cat.Contains("\"items\":")) continue;

                        string[] parts = cat.Split('"');
                        if (parts.Length < 2)
                        {
                            MelonLogger.Error("Failed to parse category name.");
                            continue;
                        }

                        string categoryName = parts[1];
                        // MelonLogger.Msg($"Category Name: {categoryName}");

                        string[] itemParts = cat.Split(new string[] { "\"items\": [" }, StringSplitOptions.None);
                        if (itemParts.Length < 2)
                        {
                            MelonLogger.Error("Failed to find item list.");
                            continue;
                        }

                        itemParts[1] = itemParts[1].Replace("]", "").Replace("}", "");

                        string itemsPart = itemParts[1];
                        List<string> items = new List<string>();

                        foreach (string item in itemsPart.Split(new char[] { '"', ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            string trimmedItem = item.Replace("\n", "").Replace("\n", "").Replace("\t", "").Replace("\t", "").Trim();

                            // MelonLogger.Msg($"Trimmed Item: {trimmedItem}");

                            if (trimmedItem == "]" || trimmedItem == "}" || trimmedItem == "{" || trimmedItem == "[")
                                break;

                            if (!string.IsNullOrWhiteSpace(trimmedItem) && !trimmedItem.Contains("["))
                            {
                                items.Add(trimmedItem);
                            }
                        }

                        // MelonLogger.Msg($"Category '{categoryName}' has {items.Count} items.");
                        itemTree[categoryName] = items;
                    }
                }
            }

            return itemTree;
        }
    }
}
