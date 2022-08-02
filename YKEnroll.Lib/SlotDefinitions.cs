using System.Collections.Generic;
using Yubico.YubiKey.Piv;

namespace YKEnroll.Lib;

/// <summary>
///  List of available and supported slots.
/// </summary>
public static class SlotDefinitions
{
    public static List<(string Name, byte SlotNumber, int DataTag)> AllAvailableSlots = new()
    {
        ("PIV Authentication", PivSlot.Authentication, (int)PivDataTag.Authentication),
        ("Digital Signature", PivSlot.Signing, (int)PivDataTag.Signature),
        ("Key Management", PivSlot.KeyManagement, (int)PivDataTag.KeyManagement),
        ("Card Authentication", PivSlot.CardAuthentication, (int)PivDataTag.CardAuthentication),
        ("Retired1", PivSlot.Retired1, (int)PivDataTag.Retired1),
        ("Retired2", PivSlot.Retired2, (int)PivDataTag.Retired2),
        ("Retired3", PivSlot.Retired3, (int)PivDataTag.Retired3),
        ("Retired4", PivSlot.Retired4, (int)PivDataTag.Retired4),
        ("Retired5", PivSlot.Retired5, (int)PivDataTag.Retired5),
        ("Retired6", PivSlot.Retired6, (int)PivDataTag.Retired6),
        ("Retired7", PivSlot.Retired7, (int)PivDataTag.Retired7),
        ("Retired8", PivSlot.Retired8, (int)PivDataTag.Retired8),
        ("Retired9", PivSlot.Retired9, (int)PivDataTag.Retired9),
        ("Retired10", PivSlot.Retired10, (int)PivDataTag.Retired10),
        ("Retired11", PivSlot.Retired11, (int)PivDataTag.Retired11),
        ("Retired12", PivSlot.Retired12, (int)PivDataTag.Retired12),
        ("Retired13", PivSlot.Retired13, (int)PivDataTag.Retired13),
        ("Retired14", PivSlot.Retired14, (int)PivDataTag.Retired14),
        ("Retired15", PivSlot.Retired15, (int)PivDataTag.Retired15),
        ("Retired16", PivSlot.Retired16, (int)PivDataTag.Retired16),
        ("Retired17", PivSlot.Retired17, (int)PivDataTag.Retired17),
        ("Retired18", PivSlot.Retired18, (int)PivDataTag.Retired18),
        ("Retired19", PivSlot.Retired19, (int)PivDataTag.Retired19),
        ("Retired20", PivSlot.Retired20, (int)PivDataTag.Retired20)
    };
}