using System;
using System.Security.Principal;

namespace YKEnroll.Lib;

/// <summary>
///     Simple class to hold information about
///     a directory user account.
/// </summary>
public class ADUser
{
    public string SecurityIdentifier { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
    public string UserPrincipalName { get; set; } = string.Empty;
    public string CommonName { get; set; } = string.Empty;
    public string GivenName { get; set; } = string.Empty;
    public string SurName { get; set; } = string.Empty;
    public string SamAccountName { get; set; } = string.Empty;
    public string Mail { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}