using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

public static class ValidateCPFFunction
{
    [FunctionName("ValidateCPFFunction")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("Validating CPF...");

        try
        {
            // Lendo o corpo da solicitação
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<RequestData>(requestBody);

            if (data == null || string.IsNullOrWhiteSpace(data.CPF))
            {
                return new BadRequestObjectResult(new { message = "CPF is required." });
            }

            // Removendo caracteres não numéricos
            string cpf = Regex.Replace(data.CPF, @"[^\d]", "");

            // Validando o CPF
            bool isValid = ValidateCPF(cpf);

            return new OkObjectResult(new
            {
                CPF = data.CPF,
                IsValid = isValid,
                Message = isValid ? "CPF is valid." : "CPF is invalid."
            });
        }
        catch (Exception ex)
        {
            log.LogError($"Error validating CPF: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    private static bool ValidateCPF(string cpf)
    {
        if (cpf.Length != 11 || !Regex.IsMatch(cpf, @"^\d+$")) return false;

        // Verifica se todos os dígitos são iguais (ex: 111.111.111-11)
        if (new string(cpf[0], cpf.Length) == cpf) return false;

        // Calcula os dígitos verificadores
        for (int i = 9; i < 11; i++)
        {
            int sum = 0;
            for (int j = 0; j < i; j++)
                sum += (cpf[j] - '0') * ((i + 1) - j);

            int remainder = (sum * 10) % 11;
            if (remainder == 10) remainder = 0;

            if (remainder != (cpf[i] - '0')) return false;
        }

        return true;
    }
}

public class RequestData
{
    public string CPF { get; set; }
}
