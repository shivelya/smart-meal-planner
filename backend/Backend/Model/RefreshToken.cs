public class RefreshToken
{
    public int Id { get; set; }               // Primary key
    public string? Token { get; set; }         // Random opaque string
    public int UserId { get; set; }        // FK to your User table
    public DateTime Expires { get; set; }     // Expiration date/time
    public bool IsRevoked { get; set; }       // Mark if the token is revoked
    public DateTime Created { get; set; }     // Creation timestamp
    public string? CreatedByIp { get; set; }  // IP address that created the token
}
