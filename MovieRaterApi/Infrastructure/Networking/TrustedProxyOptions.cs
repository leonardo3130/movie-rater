using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace MovieRaterApi.Infrastructure.Networking;

/// <summary>
/// Reverse proxies (nginx, cloudflared) allowed to report the original client IP through
/// X-Forwarded-For. Only requests arriving from these networks have the header honoured,
/// so a client cannot spoof its IP to dodge the per-IP rate limits.
/// </summary>
public class TrustedProxyOptions
{
    public const string SectionName = "TrustedProxies";

    // CIDR ranges, e.g. "172.16.0.0/12" for the default Docker bridge networks.
    public List<string> KnownNetworks { get; set; } = [];

    public static void Apply(ForwardedHeadersOptions options, TrustedProxyOptions trusted)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;

        // Each proxy hop is expected to overwrite (not append to) X-Forwarded-For,
        // so only the last entry is trusted.
        options.ForwardLimit = 1;

        foreach (var network in trusted.KnownNetworks)
        {
            if (!System.Net.IPNetwork.TryParse(network, out var parsed))
                throw new InvalidOperationException(
                    $"TrustedProxies:KnownNetworks contains an invalid CIDR range: '{network}'."
                );

            options.KnownIPNetworks.Add(parsed);
        }
    }
}
