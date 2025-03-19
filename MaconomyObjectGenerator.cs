using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VoldeligCLI
{
    public static class MaconomyObjectGenerator
    {
        public static void GenerateClassFiles()
        {
            // Read appsettings.json
            string configJson;
            try
            {
                configJson = File.ReadAllText("appsettings.json");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Error: appsettings.json not found in the current directory.");
                return;
            }

            // Parse JSON
            using JsonDocument doc = JsonDocument.Parse(configJson);
            var root = doc.RootElement;

            // Check if Maconomy and containers exist
            if (!root.TryGetProperty("Maconomy", out var maconomy) ||
                !maconomy.TryGetProperty("containers", out var containers))
            {
                Console.WriteLine("Error: Required JSON structure not found in appsettings.json");
                return;
            }

            // Create output directory if it doesn't exist
            Directory.CreateDirectory("Generated");

            // Process each container
            foreach (var container in containers.EnumerateObject())
            {
                string containerName = container.Name;

                // Skip if we don't have card configuration
                if (!container.Value.TryGetProperty("card", out var card) ||
                    !card.TryGetProperty("keyfield", out var keyfield) ||
                    !card.TryGetProperty("fields", out var fields))
                {
                    continue;
                }

                // Get the keyfield
                string keyFieldName = keyfield.GetString();

                // Get the fields
                List<string> fieldList = new List<string>();
                foreach (var field in fields.EnumerateArray())
                {
                    fieldList.Add(field.GetString());
                }

                // Generate the class file
                string classContent = GenerateClassContent(containerName, keyFieldName, fieldList);

                // Write to file
                string fileName = $"Generated/{ToPascalCase(containerName)}.cs";
                File.WriteAllText(fileName, classContent);

                Console.WriteLine($"Generated {fileName}");
            }
        }

        public static string GenerateClassContent(string containerName, string keyFieldName, List<string> fields)
        {
            string className = ToPascalCase(containerName);
            string keyFieldPropertyName = ToPascalCase(keyFieldName);

            StringBuilder sb = new StringBuilder();

            // Add using statements
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Text.Json.Serialization;");
            sb.AppendLine("using Newtonsoft.Json.Linq;");
            sb.AppendLine();

            // Determine if we need enums
            bool hasGender = fields.Contains("gender");
            bool hasCountry = fields.Contains("country");

            // Start class definition
            sb.AppendLine($"public class {className}");
            sb.AppendLine("{");

            // Add properties
            sb.AppendLine($"    [KeyField]");
            sb.AppendLine($"    public string {keyFieldPropertyName} {{ get; set; }}");

            foreach (var field in fields)
            {
                if (field == keyFieldName) continue; // Skip the key field as it's already added

                string propertyName = ToPascalCase(field);

                if (field == "gender")
                {
                    sb.AppendLine($"    [JsonConverter(typeof(SafeEnumConverter<GenderType>))]");
                    sb.AppendLine($"    public GenderType Gender {{ get; set; }}");
                }
                else if (field == "country")
                {
                    sb.AppendLine($"    [JsonConverter(typeof(SafeEnumConverter<CountryType>))]");
                    sb.AppendLine($"    public CountryType Country {{ get; set; }}");
                }
                else
                {
                    sb.AppendLine($"    public string {propertyName} {{ get; set; }}");
                }
            }

            // Add CreatedDate which is in the example but not in the fields
            sb.AppendLine($"    public DateTime CreatedDate {{ get; set; }}");

            // Add InstancesJObject method
            sb.AppendLine();
            sb.AppendLine("    public static string InstancesJObject()");
            sb.AppendLine("    {");
            sb.AppendLine("        JObject jsonObject = new JObject");
            sb.AppendLine("        {");
            sb.AppendLine("            [\"panes\"] = new JObject");
            sb.AppendLine("            {");
            sb.AppendLine("                [\"card\"] = new JObject");
            sb.AppendLine("                {");
            sb.AppendLine("                    [\"fields\"] = new JArray");
            sb.AppendLine("                            {");

            // Add fields to JArray
            sb.AppendLine($"                                \"{keyFieldName}\",");
            foreach (var field in fields)
            {
                if (field == keyFieldName) continue; // Skip the key field as it's already added
                sb.AppendLine($"                                \"{field}\",");
            }
            sb.AppendLine("                                \"createddate\",");

            sb.AppendLine("                            }");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("        };");
            sb.AppendLine("        return jsonObject.ToString();");
            sb.AppendLine("    }");

            // Add FilterJObject method
            sb.AppendLine();
            sb.AppendLine("    public static string FilterJObject(string expr, int limit)");
            sb.AppendLine("    {");
            sb.AppendLine("        JObject jsonObject = new JObject");
            sb.AppendLine("        {");
            sb.AppendLine("            [\"restriction\"] = $\"{expr}\",");
            sb.AppendLine("            [\"fields\"] = new JArray");
            sb.AppendLine("                    {");

            // Add fields to JArray
            sb.AppendLine($"                        \"{keyFieldName}\",");
            foreach (var field in fields)
            {
                if (field == keyFieldName) continue; // Skip the key field as it's already added
                sb.AppendLine($"                        \"{field}\",");
            }
            sb.AppendLine("                        \"createddate\",");

            sb.AppendLine("                    },");
            sb.AppendLine("            [\"limit\"] = limit");
            sb.AppendLine("        };");
            sb.AppendLine("        return jsonObject.ToString();");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            // Add enums if needed
            if (hasCountry)
            {
                sb.AppendLine();
                sb.AppendLine("public enum CountryType");
                sb.AppendLine("{");
                sb.AppendLine("    COUNTRY_NOT_HERE,");
                sb.AppendLine("    DENMARK,");
                sb.AppendLine("    UK,");
                sb.AppendLine("    SWEDEN,");
                sb.AppendLine("    NORWAY,");
                sb.AppendLine("    FINLAND");
                sb.AppendLine("}");
            }

            if (hasGender)
            {
                sb.AppendLine();
                sb.AppendLine("public enum GenderType");
                sb.AppendLine("{");
                sb.AppendLine("    GENDER_NOT_HERE,");
                sb.AppendLine("    MALE,");
                sb.AppendLine("    FEMALE,");
                sb.AppendLine("    NIL");
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        public static string ToPascalCase(string input)
        {
            // Handle empty or null input
            if (string.IsNullOrEmpty(input))
                return input;

            // Split the input string by non-alphanumeric characters
            var words = Regex.Split(input, @"[^a-zA-Z0-9]")
                            .Where(w => !string.IsNullOrEmpty(w))
                            .ToArray();

            // Capitalize the first letter of each word
            for (int i = 0; i < words.Length; i++)
            {
                if (!string.IsNullOrEmpty(words[i]))
                {
                    char[] letters = words[i].ToCharArray();
                    letters[0] = char.ToUpper(letters[0]);
                    words[i] = new string(letters);
                }
            }

            // Join the words back together
            return string.Join("", words);
        }
    }
}