namespace NarrationApp.Web.Tests.Configuration;

public sealed class ServerDeploymentSourceTests
{
    [Fact]
    public void Server_program_enables_forwarded_headers_for_reverse_proxy_hosts()
    {
        var projectRoot = GetProjectRoot();
        var programPath = Path.Combine(projectRoot, "src", "NarrationApp.Server", "Program.cs");
        var source = File.ReadAllText(programPath);

        Assert.Contains("using Microsoft.AspNetCore.HttpOverrides;", source, StringComparison.Ordinal);
        Assert.Contains("builder.Services.Configure<ForwardedHeadersOptions>", source, StringComparison.Ordinal);
        Assert.Contains("ForwardedHeaders.XForwardedFor", source, StringComparison.Ordinal);
        Assert.Contains("ForwardedHeaders.XForwardedProto", source, StringComparison.Ordinal);
        Assert.Contains("ForwardedHeaders.XForwardedHost", source, StringComparison.Ordinal);
        Assert.Contains("app.UseForwardedHeaders();", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Repo_includes_reverse_proxy_samples_and_deploy_doc_references_them()
    {
        var projectRoot = GetProjectRoot();
        var nginxPath = Path.Combine(projectRoot, "deploy", "reverse-proxy", "nginx", "narrationapp.conf");
        var iisPath = Path.Combine(projectRoot, "deploy", "reverse-proxy", "iis", "web.config");
        var iisScriptPath = Path.Combine(projectRoot, "scripts", "configure_iis_reverse_proxy.ps1");
        var iisPrereqScriptPath = Path.Combine(projectRoot, "scripts", "install_iis_prerequisites.ps1");
        var reverseProxyDocPath = Path.Combine(projectRoot, "docs", "reverse-proxy-setup.md");
        var deployDocPath = Path.Combine(projectRoot, "docs", "server-production-deploy.md");

        Assert.True(File.Exists(nginxPath), "nginx sample config should exist.");
        Assert.True(File.Exists(iisPath), "IIS sample config should exist.");
        Assert.True(File.Exists(iisScriptPath), "IIS helper script should exist.");
        Assert.True(File.Exists(iisPrereqScriptPath), "IIS prerequisite install script should exist.");
        Assert.True(File.Exists(reverseProxyDocPath), "Reverse proxy setup doc should exist.");

        var nginxSource = File.ReadAllText(nginxPath);
        Assert.Contains("proxy_pass http://127.0.0.1:5000;", nginxSource, StringComparison.Ordinal);
        Assert.Contains("X-Forwarded-Proto", nginxSource, StringComparison.Ordinal);

        var iisSource = File.ReadAllText(iisPath);
        Assert.Contains("HTTP_X_FORWARDED_PROTO", iisSource, StringComparison.Ordinal);
        Assert.Contains("http://127.0.0.1:5000/{R:1}", iisSource, StringComparison.Ordinal);

        var deployDoc = File.ReadAllText(deployDocPath);
        Assert.Contains("reverse-proxy-setup.md", deployDoc, StringComparison.Ordinal);
        Assert.Contains("deploy/reverse-proxy/nginx/narrationapp.conf", deployDoc, StringComparison.Ordinal);
        Assert.Contains("deploy/reverse-proxy/iis/web.config", deployDoc, StringComparison.Ordinal);
        Assert.Contains("scripts/configure_iis_reverse_proxy.ps1", deployDoc, StringComparison.Ordinal);
        Assert.Contains("scripts/install_iis_prerequisites.ps1", deployDoc, StringComparison.Ordinal);

        var reverseProxyDoc = File.ReadAllText(reverseProxyDocPath);
        Assert.Contains("scripts/configure_iis_reverse_proxy.ps1", reverseProxyDoc, StringComparison.Ordinal);
        Assert.Contains("Install-WindowsFeature", reverseProxyDoc, StringComparison.Ordinal);
        Assert.Contains("Enable-WindowsOptionalFeature", reverseProxyDoc, StringComparison.Ordinal);
        Assert.Contains("Windows 10/11", reverseProxyDoc, StringComparison.Ordinal);

        var iisScript = File.ReadAllText(iisScriptPath);
        Assert.Contains("Import-Module WebAdministration", iisScript, StringComparison.Ordinal);
        Assert.Contains("Install-WindowsFeature Web-Server, Web-Mgmt-Tools, Web-Scripting-Tools", iisScript, StringComparison.Ordinal);
        Assert.Contains("Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole, IIS-ManagementScriptingTools", iisScript, StringComparison.Ordinal);
        Assert.Contains("Set-WebConfigurationProperty", iisScript, StringComparison.Ordinal);
        Assert.Contains("New-Item -ItemType Directory", iisScript, StringComparison.Ordinal);

        var iisPrereqScript = File.ReadAllText(iisPrereqScriptPath);
        Assert.Contains("Import-Module ServerManager", iisPrereqScript, StringComparison.Ordinal);
        Assert.Contains("Install-WindowsFeature", iisPrereqScript, StringComparison.Ordinal);
        Assert.Contains("Enable-WindowsOptionalFeature", iisPrereqScript, StringComparison.Ordinal);
        Assert.Contains("IIS-WebServerRole", iisPrereqScript, StringComparison.Ordinal);
        Assert.Contains("Web-Scripting-Tools", iisPrereqScript, StringComparison.Ordinal);
    }

    private static string GetProjectRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }
}
