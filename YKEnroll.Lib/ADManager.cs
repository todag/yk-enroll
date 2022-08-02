using System.DirectoryServices;
using System.Security.Principal;

namespace YKEnroll.Lib;

public static class ADManager
{
    /// <summary>
    ///     Returns list of AD users. The results will
    ///     be filtered on the string provided in searchString.
    ///     The following attributes will be wildcard matched
    ///     against the searchString:
    ///     cn,
    ///     displayName,
    ///     sn,
    ///     givenName,
    ///     samAccountName
    /// </summary>
    /// <param name="searchString">This string will be rendered into an ldap searchstring</param>
    /// <returns></returns>
    public static List<ADUser> FindUsers(string searchString)
    {
        var users = new List<ADUser>();
        if (searchString == null || string.IsNullOrWhiteSpace(searchString))
        {
            searchString = "*";
        }
        else
        {
            searchString = searchString.StartsWith("*") ? searchString : "*" + searchString;
            searchString = searchString.EndsWith("*") ? searchString : searchString + "*";
        }

        searchString =
            string.Format(
                "(&(objectClass=user)(objectCategory=person)(|(cn={0})(displayName={0})(sn={0})(givenName={0})(mail={0})(samAccountName={0})))", searchString);

        using (var directoryEntry = new DirectoryEntry(@"GC://rootDSE"))
        {
            // Create a Global Catalog Directory Service Searcher
            var strRootName = directoryEntry.Properties["rootDomainNamingContext"].Value!.ToString();
            using (var usersBinding = new DirectoryEntry(@"GC://" + strRootName))
            {
                var searcher = new DirectorySearcher(usersBinding);
                searcher.Filter = searchString;
                searcher.SearchScope = SearchScope.Subtree;
                searcher.PropertiesToLoad.Add("distinguishedName");
                searcher.PropertiesToLoad.Add("objectSid");
                searcher.PropertiesToLoad.Add("samAccountName");
                searcher.PropertiesToLoad.Add("cn");
                searcher.PropertiesToLoad.Add("userPrincipalName");
                searcher.PropertiesToLoad.Add("displayName");
                searcher.PropertiesToLoad.Add("sn");
                searcher.PropertiesToLoad.Add("givenName");
                searcher.PropertiesToLoad.Add("mail");
                searcher.PropertiesToLoad.Add("mobile");
                searcher.PropertiesToLoad.Add("name");
                searcher.PropertiesToLoad.Add("telephoneNumber");
                searcher.ClientTimeout = new TimeSpan(0, 0, 10);
                searcher.CacheResults = true;

                foreach (SearchResult result in searcher.FindAll())
                {
                    ADUser user = new();
                    if (result.Properties["distinguishedName"].Count > 0)
                        user.DistinguishedName = result.Properties["distinguishedName"][0].ToString() ?? string.Empty;
                    if (result.Properties["objectSid"].Count > 0)
                        user.SecurityIdentifier = new SecurityIdentifier((byte[])result.Properties["objectSid"][0], 0).Value;
                    if (result.Properties["samAccountName"].Count > 0)
                        user.SamAccountName = result.Properties["samAccountName"][0].ToString() ?? string.Empty;
                    if (result.Properties["cn"].Count > 0)
                        user.CommonName = result.Properties["cn"][0].ToString() ?? string.Empty;
                    if (result.Properties["userPrincipalName"].Count > 0)
                        user.UserPrincipalName = result.Properties["userPrincipalName"][0].ToString() ?? string.Empty;
                    if (result.Properties["displayName"].Count > 0)
                        user.DisplayName = result.Properties["displayName"][0].ToString() ?? string.Empty;
                    if (result.Properties["sn"].Count > 0)
                        user.SurName = result.Properties["sn"][0].ToString() ?? string.Empty;
                    if (result.Properties["givenName"].Count > 0)
                        user.GivenName = result.Properties["givenName"][0].ToString() ?? string.Empty;
                    if (result.Properties["mail"].Count > 0)
                        user.Mail = result.Properties["mail"][0].ToString() ?? string.Empty;
                    if (result.Properties["mobile"].Count > 0)
                        user.Mobile = result.Properties["mobile"][0].ToString() ?? string.Empty;
                    if (result.Properties["name"].Count > 0)
                        user.Name = result.Properties["name"][0].ToString() ?? string.Empty;
                    users.Add(user);
                }
            }
        }
        return users;
    }

    /// <summary>
    ///     Searches the CN=Enrollment Services,CN=Public Key Services,CN=Services,...
    ///     container and returns the result as a list of CA Servers.    
    /// </summary>
    /// <returns></returns>
    public static List<CAServer> GetCAServers()
    {
        List<CAServer> caServers = new();
        using (var directoryEntry = new DirectoryEntry(@"GC://rootDSE"))
        {                        
            var strRootName = directoryEntry.Properties["configurationNamingContext"].Value!.ToString();

            using (var entry =
                   new DirectoryEntry("LDAP://CN=Enrollment Services,CN=Public Key Services,CN=Services," +
                                      strRootName))
            {
                foreach (DirectoryEntry result in entry.Children)
                {
                    CAServer caServer = new();
                    if (result.Properties["displayName"].Count > 0)
                        caServer.DisplayName = result.Properties["displayName"][0]!.ToString() ?? string.Empty;
                    if (result.Properties["dNSHostName"].Count > 0)
                        caServer.DnsHostName = result.Properties["dNSHostName"][0]!.ToString() ?? string.Empty;
                    if (result.Properties["cn"].Count > 0)
                        caServer.CommonName = result.Properties["cn"][0]!.ToString() ?? string.Empty;
                    
                    Logger.Log($"Found CA server: \n\"displayName:{caServer.DisplayName}\" \n\"dns:{caServer.DnsHostName}\" \n\"cn:{caServer.CommonName}\" ");                    
                    caServer.CertificateTemplates = GetCertTemplates();
                    caServers.Add(caServer);
                }
            }
        }
        return caServers;
    }
       
    /// <summary>
    ///     Searaches the CN=Certificate Templates,CN=Public Key Services,CN=Services,...
    ///     container and returns relevant information as a list of CertificateTemplates.
    /// </summary>
    /// <returns></returns>
    public static List<CertificateTemplate> GetCertTemplates()
    {
        var certificateTemplates = new List<CertificateTemplate>();
        using (var directoryEntry = new DirectoryEntry(@"GC://rootDSE"))
        {
            var strRootName = directoryEntry.Properties["configurationNamingContext"].Value!.ToString();
            using (var entry =
                   new DirectoryEntry("LDAP://CN=Certificate Templates,CN=Public Key Services,CN=Services," +
                                      strRootName))
            {
                foreach (DirectoryEntry result in entry.Children)
                {
                    var certificateTemplate = new CertificateTemplate();
                    if (result.Properties["displayName"].Count > 0)
                        certificateTemplate.DisplayName = result.Properties["displayName"][0]!.ToString() ?? string.Empty;
                    if (result.Properties["name"].Count > 0)
                        certificateTemplate.Name = result.Properties["name"][0]!.ToString() ?? string.Empty;
                    if (result.Properties["msPKI-RA-Signature"].Count > 0)
                        certificateTemplate.RequiredSignatures = Convert.ToInt32(result.Properties["msPKI-RA-Signature"][0]);
                    
                    // Process oid filter                        
                    if (!Settings.IncludeTemplateOid.All(itm2 =>
                            result.Properties["msPKI-Certificate-Application-Policy"].Contains(itm2)))
                    {
                        Logger.Log($"Ignoring template \"{certificateTemplate.DisplayName}\" since oid is not in Settings.IncludeTemplateOid.");
                        continue;
                    }
                        
                    if (Settings.ExcludeTemplateOid.Any(itm2 =>
                            result.Properties["msPKI-Certificate-Application-Policy"].Contains(itm2)))
                    {                        
                        Logger.Log($"Ignoring template \"{certificateTemplate.DisplayName}\" since oid is in ExcludeTemplateOid.");
                        continue;
                    }

                    if (!(Settings.IncludeTemplateName.Length == 0) &&
                        !Settings.IncludeTemplateName.Contains(certificateTemplate.Name))
                    {
                        Logger.Log($"Ignoring template \"{certificateTemplate.Name}\" since name is not in Settings.IncludeTemplateName (And it's not empty).");
                        continue;
                    }
                        
                    certificateTemplates.Add(certificateTemplate);
                }
            }
        }
        return certificateTemplates;
    }
}