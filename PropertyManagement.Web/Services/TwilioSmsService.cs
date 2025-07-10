using Twilio;
using Twilio.Rest.Api.V2010.Account;

public class TwilioSmsService : ISmsService
{
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromNumber;

    public TwilioSmsService(IConfiguration config)
    {
        _accountSid = config["Twilio:AccountSid"];
        _authToken = config["Twilio:AuthToken"];
        _fromNumber = config["Twilio:FromNumber"];
    }

    public async Task SendAsync(string phoneNumber, string message)
    {
        TwilioClient.Init(_accountSid, _authToken);
        await MessageResource.CreateAsync(
            body: message,
            from: new Twilio.Types.PhoneNumber(_fromNumber),
            to: new Twilio.Types.PhoneNumber(phoneNumber)
        );
    }
}