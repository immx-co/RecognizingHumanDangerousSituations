using Avalonia.Collections;
using System.Text.Json.Serialization;

namespace ClassLibrary.Services
{
    public class JsonItem
    {
        [JsonPropertyName("frame_time")]
        public TimeSpan FrameTime { get; set; }

        [JsonPropertyName("rect_items")]
        public AvaloniaList<RectItem> RectItems { get; set; } = new();

        [JsonPropertyName("fig_items")]
        public AvaloniaList<FigItem> FigItems { get; set; } = new();

    }
}
