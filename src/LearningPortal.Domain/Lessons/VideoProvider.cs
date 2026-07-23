namespace LearningPortal.Domain.Lessons;

/// <summary>Defines supported video playback providers.</summary>
public enum VideoProvider
{
    /// <summary>No video provider.</summary>
    None,
    /// <summary>YouTube video.</summary>
    YouTube,
    /// <summary>Vimeo video.</summary>
    Vimeo,
    /// <summary>Microsoft Stream or SharePoint video.</summary>
    MicrosoftStream,
    /// <summary>Direct HTTPS MP4 video.</summary>
    DirectMp4
}
