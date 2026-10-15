using System.Collections.Generic;
using System.Linq;
using Mortar.Core.Authoring;
using Mortar.Json.Content;

namespace Mortar.Unity
{
    public static class ContentLoading
    {
        public const int ShownErrors = 5;

        public static GameContent? Load(string manifest, out string? error)
        {
            var result = GameContentLoader.Load(manifest);
            error = result.Succeeded ? null : Describe(result.Errors);
            return result.Value;
        }

        public static string Describe(IReadOnlyList<AuthoringError> errors)
        {
            var lines = errors.Take(ShownErrors).Select(error => error.ToString()).ToList();
            if (errors.Count > ShownErrors)
            {
                lines.Add($"... and {errors.Count - ShownErrors} more");
            }
            return "Content failed to load:\n" + string.Join("\n", lines);
        }
    }
}
