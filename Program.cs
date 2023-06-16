using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

class Keygen
{
  private RestClient client = null;

  public Keygen(string accountId)
  {
    client = new RestClient($"https://api.keygen.sh/v1/accounts/{accountId}");
  }

  async public Task<Document<License, Validation>> ValidateLicense(string licenseKey, string deviceFingerprint)
  {
    var request = new RestRequest("licenses/actions/validate-key", Method.Post);

    request.AddHeader("Content-Type", "application/vnd.api+json");
    request.AddHeader("Accept", "application/vnd.api+json");
    request.AddJsonBody(new {
      meta = new {
        key = licenseKey,
        scope = new {
          fingerprint = deviceFingerprint,
        }
      }
    });

    var response = await client.ExecuteAsync<Document<License, Validation>>(request);
    if (response.Data.Errors.Count > 0)
    {
      var err = response.Data.Errors[0];

      Console.WriteLine("[ERROR] [ValidateLicense] Status={0} Title={1} Detail={2} Code={3}", response.StatusCode, err.Title, err.Detail, err.Code);

      Environment.Exit(1);
    }

    return response.Data;
  }

  async public Task<Document<Machine>> ActivateDevice(string licenseId, string licenseKey, string deviceFingerprint)
  {
    var request = new RestRequest("machines", Method.Post);

    request.AddHeader("Authorization", $"License {licenseKey}");
    request.AddHeader("Content-Type", "application/vnd.api+json");
    request.AddHeader("Accept", "application/vnd.api+json");
    request.AddJsonBody(new {
      data = new {
        type = "machine",
        attributes = new {
          fingerprint = deviceFingerprint,
        },
        relationships = new {
          license = new {
            data = new {
              type = "license",
              id = licenseId,
            }
          }
        }
      }
    });

    var response = await client.ExecuteAsync<Document<Machine>>(request);
    if (response.Data.Errors.Count > 0)
    {
      var err = response.Data.Errors[0];

      Console.WriteLine("[ERROR] [ActivateDevice] Status={0} Title={1} Detail={2} Code={3}", response.StatusCode, err.Title, err.Detail, err.Code);

      Environment.Exit(1);
    }

    return response.Data;
  }

  public class Document<T>
  {
    public T Data { get; set; }
    public List<Error> Errors { get; set; } = new();
  }

  public class Document<T, U> : Document<T>
  {
    public U Meta { get; set; }
  }

  public class Error
  {
    public string Title { get; set; }
    public string Detail { get; set; }
    public string Code { get; set; }
  }

  public class Validation
  {
    public Boolean Valid { get; set; }
    public string Detail { get; set; }
    [JsonPropertyNameAttribute("code")]
    public string Code { get; set; }
  }

  public class License
  {
    public string Type { get; set; }
    public string ID { get; set; }
  }

  public class Machine
  {
    public string Type { get; set; }
    public string ID { get; set; }
  }
}

class Program
{
  async public static Task MainAsync (string[] args)
  {
    var keygen = new Keygen("demo");

    // Keep a reference to the current license and device
    Keygen.License license = null;
    Keygen.Machine device = null;

    // Validate license
    var validation = await keygen.ValidateLicense("0BB042-E1A90B-A5DC67-D651E0-73E6C5-V3", "AB:CD:EF:GH:IJ:KL:MN:OP");
    if (validation.Meta.Valid)
    {
      Console.WriteLine("[INFO] [ValidateLicense] Valid={0} ValidationCode={1}", validation.Meta.Detail, validation.Meta.Code);
    }
    else
    {
      Console.WriteLine("[INFO] [ValidateLicense] Invalid={0} ValidationCode={1}", validation.Meta.Detail, validation.Meta.Code);
    }

    // Store license data
    license = validation.Data;

    // Activate the current machine if it is not already activated (based on validation code)
    switch (validation.Meta.Code)
    {
      case "FINGERPRINT_SCOPE_MISMATCH":
      case "NO_MACHINES":
      case "NO_MACHINE":
        var activation = await keygen.ActivateDevice((string) license.ID, "0BB042-E1A90B-A5DC67-D651E0-73E6C5-V3", "AB:CD:EF:GH:IJ:KL:MN:OP");

        // Store device data
        device = activation.Data;

        Console.WriteLine("[INFO] [ActivateDevice] DeviceId={0} LicenseId={1}", device.ID, license.ID);

        // OPTIONAL: Validate license again
        validation = await keygen.ValidateLicense("0BB042-E1A90B-A5DC67-D651E0-73E6C5-V3", "AB:CD:EF:GH:IJ:KL:MN:OP");
        if (validation.Meta.Valid)
        {
          Console.WriteLine("[INFO] [ValidateLicense] Valid={0} ValidationCode={1}", validation.Meta.Detail, validation.Meta.Code);
        }
        else
        {
          Console.WriteLine("[INFO] [ValidateLicense] Invalid={0} ValidationCode={1}", validation.Meta.Detail, validation.Meta.Code);
        }

        break;
    }

    // Print the overall results
    Console.WriteLine("[INFO] [Main] Valid={0} RecentlyActivated={1}", validation.Meta.Valid, device != null);
  }

  public static void Main (string[] args)
  {
    MainAsync(args).GetAwaiter().GetResult();
  }
}
