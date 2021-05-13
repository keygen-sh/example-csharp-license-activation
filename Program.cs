using RestSharp;
using System;
using System.Collections.Generic;

class Keygen
{
  public RestClient Client = null;

  public Keygen(string accountId)
  {
    Client = new RestClient($"https://api.keygen.sh/v1/accounts/{accountId}");
  }

  public Dictionary<string, object> ValidateLicense(string licenseKey, string deviceFingerprint)
  {
    var request = new RestRequest("licenses/actions/validate-key", Method.POST);

    request.AddHeader("Content-Type", "application/vnd.api+json");
    request.AddHeader("Accept", "application/vnd.api+json");
    request.AddJsonBody(new {
      meta = new {
        key = licenseKey,
        scope = new {
          fingerprint = deviceFingerprint
        }
      }
    });

    var response = Client.Execute<Dictionary<string, object>>(request);
    if (response.Data.ContainsKey("errors"))
    {
      var errors = (RestSharp.JsonArray) response.Data["errors"];
      if (errors != null)
      {
        Console.WriteLine("[ERROR] [ValidateLicense] Status={0} Errors={1}", response.StatusCode, errors);

        Environment.Exit(1);
      }
    }

    return response.Data;
  }

  public Dictionary<string, object> ActivateDevice(string licenseId, string deviceFingerprint, string activationToken)
  {
    var request = new RestRequest("machines", Method.POST);

    request.AddHeader("Authorization", $"Bearer {activationToken}");
    request.AddHeader("Content-Type", "application/vnd.api+json");
    request.AddHeader("Accept", "application/vnd.api+json");
    request.AddJsonBody(new {
      data = new {
        type = "machine",
        attributes = new {
          fingerprint = deviceFingerprint
        },
        relationships = new {
          license = new {
            data = new {
              type = "license",
              id = licenseId
            }
          }
        }
      }
    });

    var response = Client.Execute<Dictionary<string, object>>(request);
    if (response.Data.ContainsKey("errors"))
    {
      var errors = (RestSharp.JsonArray) response.Data["errors"];
      if (errors != null)
      {
        Console.WriteLine("[ERROR] [ActivateDevice] Status={0} Errors={1}", response.StatusCode, errors);

        Environment.Exit(1);
      }
    }

    return response.Data;
  }
}

class Program
{
  public static void Main (string[] args)
  {
    Dictionary<string, object> license = null;
    Dictionary<string, object> device = null;
    var keygen = new Keygen("demo");

    // Validate license
    var validation = keygen.ValidateLicense("0BB042-E1A90B-A5DC67-D651E0-73E6C5-V3", "AB:CD:EF:GH:IJ:KL:MN:OP");
    var meta = (Dictionary<string, object>) validation["meta"];
    if ((bool) meta["valid"])
    {
      Console.WriteLine("[INFO] [ValidateLicense] Valid={0} ValidationCode={1}", meta["detail"], meta["constant"]);
    }
    else
    {
      Console.WriteLine("[INFO] [ValidateLicense] Invalid={0} ValidationCode={1}", meta["detail"], meta["constant"]);
    }

    // Store license data
    license = (Dictionary<string, object>) validation["data"];

    // Activate the current machine if it is not already activated (based on validation code)
    switch ((string) meta["constant"])
    {
      case "FINGERPRINT_SCOPE_MISMATCH":
      case "NO_MACHINES":
      case "NO_MACHINE":
        var activation = keygen.ActivateDevice((string) license["id"], "AB:CD:EF:GH:IJ:KL:MN:OP", "activ-37ab75d19cfbc88b6c4c4e06c3517447v3");

        // Store device data
        device = (Dictionary<string, object>) activation["data"];

        Console.WriteLine("[INFO] [ActivateDevice] DeviceId={0} LicenseId={1}", device["id"], license["id"]);

        // OPTIONAL: Validate license again
        validation = keygen.ValidateLicense("0BB042-E1A90B-A5DC67-D651E0-73E6C5-V3", "AB:CD:EF:GH:IJ:KL:MN:OP");
        meta = (Dictionary<string, object>) validation["meta"];
        if ((bool) meta["valid"])
        {
          Console.WriteLine("[INFO] [ValidateLicense] Valid={0} ValidationCode={1}", meta["detail"], meta["constant"]);
        }
        else
        {
          Console.WriteLine("[INFO] [ValidateLicense] Invalid={0} ValidationCode={1}", meta["detail"], meta["constant"]);
        }

        break;
    }

    // Print the overall results
    Console.WriteLine("[INFO] [Main] Valid={0} RecentlyActivated={1}", meta["valid"], device != null);
  }
}