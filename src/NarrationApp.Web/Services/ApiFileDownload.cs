namespace NarrationApp.Web.Services;

public sealed record ApiFileDownload(string FileName, string ContentType, byte[] Content);
