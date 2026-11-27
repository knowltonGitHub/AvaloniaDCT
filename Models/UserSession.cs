namespace AvaloniaDCT.Models; // Passed from login into MainViewModel for the rest of the process

// Immutable snapshot of "who is logged in".
// record = compiler-generated UserId, UserName, Password plus value equality.
// Password is the typed login secret. In hardened mode it is also the SQLCipher key
// cached so later connections do not ask the user again.
public sealed record UserSession(int UserId, string UserName, string Password); // UserId = user.id, UserName = login name, Password = typed secret / SQLCipher key
