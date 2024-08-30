namespace Mesh2Ical.Mesh
{
    public class MeshOptions
    {
        public string Token { get; set; } = string.Empty;

        public Dictionary<string, string> Child2File { get; } = new();
    }
}
