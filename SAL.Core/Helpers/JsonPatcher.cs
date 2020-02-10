using JsonDiffPatchDotNet;
using Newtonsoft.Json.Linq;

namespace SAL.Core.Helpers
{
    public static class JsonPatcher
    {
        private static readonly JsonDiffPatch pather;

        static JsonPatcher()
        {
            pather = new JsonDiffPatch();
        }

        public static JObject Diff(JObject left, JObject right)
        {
            if (pather.Diff(left, right) is JObject patch)
                return patch;
            return new JObject();
        }

        public static JObject Patch(JObject obj, JObject patch)
        {
            if (pather.Patch(obj, patch) is JObject newObj)
                return newObj;
            return new JObject();
        }

        public static JObject Unpatch(JObject obj, JObject patch)
        {
            if (pather.Unpatch(obj, patch) is JObject newObj)
                return newObj;
            return new JObject();
        }
    }
}
