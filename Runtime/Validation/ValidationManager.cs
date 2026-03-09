using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Playout.Registration
{

    #region Indice
    // ========================= INDICE DE FUNCIONAMENTO ========================= //
    //
    // 1. ------ Validação de Email ------ //
    
    // 2. ------ Validação de Nome ------ //
    // 2.1 ------ Formatação do Nome (iniciais maiúsculas) ------ //

    // 2.2 ------ Validação de Sobrenome + formatação ------ //

    // 3. ------ Validação de Telefone ------ //
    // 3.1 ------ Formatação de Telefone  (XX)XXXXX-XXXX ------ //
    
    // 4. ------ Validação de CPF (Módulo 11) ------ //
    // 4.1 ------ Formatação de CPF  XXX.XXX.XXX-XX ------ //

    // 5. ------ Validação de CNPJ (Módulo 11) ------ //
    // 5.1 ------ Formatação de CNPJ  XX.XXX.XXX/XXXX-XX ------ //

    // 6. ------ Validação de RG (mínimo 4 caracteres) ------ //

    // 7. ------ Validação de CRO (UF + 4-6 dígitos) ------ //
    // 7.1 ------ Formatação de CRO  UF-XXXXXX ------ //

    // 8. ------ Validação de CRM (UF + 4-6 dígitos) ------ //
    // 8.1 ------ Formatação de CRM  UF-XXXXXX ------//

    // 9. ------ Validação de Senha ------ //

    // 10. ------ Resultado da validação ------ //
    #endregion


    
    #region 1.
    public class EmailValidator : IFieldValidator
    {
        public RegistrationFieldType FieldType => RegistrationFieldType.Email;

        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public ValidationResult Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ValidationResult.Failure("O e-mail é obrigatório, preencha o campo para continuar.");

            if (!EmailRegex.IsMatch(value.Trim()))
                return ValidationResult.Failure("E-mail inválido.");

            return ValidationResult.Success();
        }
    }

    #endregion


    #region 2. 
    public class NameValidator : IFieldValidator
    {
        public RegistrationFieldType FieldType => RegistrationFieldType.Name;

        private static readonly Regex OnlyLettersAndSpaces = new Regex(
            @"^[\p{L}\s]+$",
            RegexOptions.Compiled
        );

        public ValidationResult Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ValidationResult.Failure("O nome é obrigatório.");

            if (!OnlyLettersAndSpaces.IsMatch(value.Trim()))
                return ValidationResult.Failure("O nome deve conter apenas letras.");

            return ValidationResult.Success();
        }

        #region 2.1
        /// <summary>Iniciais maiúsculas (ex.: "maria clara" → "Maria Clara"). Use no onValueChanged do input.</summary>
        public static string FormatInput(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var culture = CultureInfo.CurrentCulture;
            return culture.TextInfo.ToTitleCase(value.ToLower(culture));
        }
        #endregion
    }

    #endregion


    
    #region 2.2 
    public class LastNameValidator : IFieldValidator
    {
        public RegistrationFieldType FieldType => RegistrationFieldType.LastName;

        private static readonly Regex OnlyLettersAndSpaces = new Regex(
            @"^[\p{L}\s]+$",
            RegexOptions.Compiled
        );

        public ValidationResult Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ValidationResult.Failure("O sobrenome é obrigatório.");

            if (!OnlyLettersAndSpaces.IsMatch(value.Trim()))
                return ValidationResult.Failure("O sobrenome deve conter apenas letras.");

            return ValidationResult.Success();
        }

        /// <summary>Iniciais maiúsculas. Reutiliza NameValidator.FormatInput.</summary>
        public static string FormatInput(string value) => NameValidator.FormatInput(value);
    }

    #endregion


    
    #region 3.
    // Regras:
    //  - Apenas dígitos (remove tudo que não for número antes de validar).
    //  - Não pode começar com 0.
    //  - Mínimo 10 dígitos (fixo: 2 DDD + 8 número).
    //  - Máximo 11 dígitos (celular: 2 DDD + 9 número).

    public class PhoneValidator : IFieldValidator
    {
        public RegistrationFieldType FieldType => RegistrationFieldType.Phone;

        private static readonly Regex PhoneRegex = new Regex(
            @"^[1-9]\d{9,10}$",
            RegexOptions.Compiled
        );

        /// <summary>Remove tudo que não for dígito do valor.</summary>
        public static string StripNonDigits(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return Regex.Replace(value, @"\D", "");
        }

        public ValidationResult Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ValidationResult.Failure("O telefone é obrigatório.");

            string digits = StripNonDigits(value);

            if (digits.Length < 10)
                return ValidationResult.Failure("Telefone deve ter no mínimo 10 dígitos (DDD + número).");

            if (digits.Length > 11)
                return ValidationResult.Failure("Telefone deve ter no máximo 11 dígitos.");

            if (!PhoneRegex.IsMatch(digits))
                return ValidationResult.Failure("Telefone inválido. Não inicie com 0 e use apenas números.");

            return ValidationResult.Success();
        }

        #region 3.1 
        /// <summary>
        /// Formata o telefone para o padrão brasileiro.
        /// Celular 11 dígitos: (XX)XXXXX-XXXX  |  Fixo 10 dígitos: (XX)XXXX-XXXX
        /// Use no onValueChanged do input.
        /// </summary>
        public static string FormatInput(string value)
        {
            string digits = StripNonDigits(value);
            if (string.IsNullOrEmpty(digits)) return value;

            if (digits.Length == 11)
                return $"({digits.Substring(0, 2)}){digits.Substring(2, 5)}-{digits.Substring(7, 4)}";

            if (digits.Length == 10)
                return $"({digits.Substring(0, 2)}){digits.Substring(2, 4)}-{digits.Substring(6, 4)}";

            return value;
        }
        #endregion
    }

    #endregion


    
    #region 4.
    // Algoritmo oficial da Receita Federal:
    //  1) Remove caracteres não numéricos.
    //  2) Rejeita sequências iguais (111.111.111-11, etc.).
    //  3) Calcula o 1º dígito verificador (peso 10→2 sobre os 9 primeiros).
    //  4) Calcula o 2º dígito verificador (peso 11→2 sobre os 10 primeiros).
    //  5) Compara com os dois últimos dígitos informados.

    public class CpfValidator : IFieldValidator
    {
        public RegistrationFieldType FieldType => RegistrationFieldType.CPF;

        public static string StripNonDigits(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return Regex.Replace(value, @"\D", "");
        }

        public ValidationResult Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ValidationResult.Failure("O CPF é obrigatório.");

            string digits = StripNonDigits(value);

            if (digits.Length != 11)
                return ValidationResult.Failure("O CPF deve conter 11 dígitos.");

            if (digits.All(c => c == digits[0]))
                return ValidationResult.Failure("CPF inválido (sequência repetida).");

            if (!ValidateModulo11(digits))
                return ValidationResult.Failure("CPF inválido.");

            return ValidationResult.Success();
        }

        private static bool ValidateModulo11(string digits)
        {
            int sum = 0;
            for (int i = 0; i < 9; i++)
                sum += (digits[i] - '0') * (10 - i);
            int remainder = sum % 11;
            int firstDigit = remainder < 2 ? 0 : 11 - remainder;
            if ((digits[9] - '0') != firstDigit) return false;

            sum = 0;
            for (int i = 0; i < 10; i++)
                sum += (digits[i] - '0') * (11 - i);
            remainder = sum % 11;
            int secondDigit = remainder < 2 ? 0 : 11 - remainder;
            return (digits[10] - '0') == secondDigit;
        }

        #region 4.1
        //Formata para XXX.XXX.XXX-XX. Use no onValueChanged do input//
        public static string FormatInput(string value)
        {
            string digits = StripNonDigits(value);
            if (string.IsNullOrEmpty(digits)) return value;

            if (digits.Length == 11)
                return $"{digits.Substring(0, 3)}.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-{digits.Substring(9, 2)}";

            return value;
        }
        #endregion
    }

    #endregion


    
    #region 5.
    // Algoritmo da Receita Federal para CNPJ:
    //  1) Remove caracteres não numéricos.
    //  2) Rejeita sequências iguais.
    //  3) Calcula o 1º dígito verificador (pesos 5,4,3,2,9,8,7,6,5,4,3,2 sobre os 12 primeiros).
    //  4) Calcula o 2º dígito verificador (pesos 6,5,4,3,2,9,8,7,6,5,4,3,2 sobre os 13 primeiros).

    public class CnpjValidator : IFieldValidator
    {
        public RegistrationFieldType FieldType => RegistrationFieldType.CNPJ;

        public static string StripNonDigits(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return Regex.Replace(value, @"\D", "");
        }

        public ValidationResult Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ValidationResult.Failure("O CNPJ é obrigatório.");

            string digits = StripNonDigits(value);

            if (digits.Length != 14)
                return ValidationResult.Failure("O CNPJ deve conter 14 dígitos.");

            if (digits.All(c => c == digits[0]))
                return ValidationResult.Failure("CNPJ inválido (sequência repetida).");

            if (!ValidateModulo11(digits))
                return ValidationResult.Failure("CNPJ inválido.");

            return ValidationResult.Success();
        }

        private static bool ValidateModulo11(string digits)
        {
            int[] weights1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int sum = 0;
            for (int i = 0; i < 12; i++)
                sum += (digits[i] - '0') * weights1[i];
            int remainder = sum % 11;
            int firstDigit = remainder < 2 ? 0 : 11 - remainder;
            if ((digits[12] - '0') != firstDigit) return false;

            int[] weights2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            sum = 0;
            for (int i = 0; i < 13; i++)
                sum += (digits[i] - '0') * weights2[i];
            remainder = sum % 11;
            int secondDigit = remainder < 2 ? 0 : 11 - remainder;
            return (digits[13] - '0') == secondDigit;
        }

        #region 5.1
        //Formata para XX.XXX.XXX/XXXX-XX. Use no onValueChanged do input//
        public static string FormatInput(string value)
        {
            string digits = StripNonDigits(value);
            if (string.IsNullOrEmpty(digits)) return value;

            if (digits.Length == 14)
                return $"{digits.Substring(0, 2)}.{digits.Substring(2, 3)}.{digits.Substring(5, 3)}/{digits.Substring(8, 4)}-{digits.Substring(12, 2)}";

            return value;
        }
        #endregion
    }

    #endregion


    
    #region 6. 
    // Não há algoritmo padronizado nacionalmente. Exige apenas mínimo de 4 caracteres//
    public class RgValidator : IFieldValidator
    {
        public RegistrationFieldType FieldType => RegistrationFieldType.RG;

        public ValidationResult Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ValidationResult.Failure("O RG é obrigatório.");

            string trimmed = value.Trim();
            if (trimmed.Length < 4)
                return ValidationResult.Failure("O RG deve ter no mínimo 4 caracteres.");

            return ValidationResult.Success();
        }
    }

    #endregion


    
    #region 7
    // Estrutura: UF seguida de 4 a 6 dígitos. Aceita separadores: hífen, espaço ou nada.
    // Exemplos válidos: SP-123456, RJ 12345, MG1234

    public class CroValidator : IFieldValidator
    {
        public RegistrationFieldType FieldType => RegistrationFieldType.CRO;

        private static readonly string[] ValidUFs =
        {
            "AC","AL","AP","AM","BA","CE","DF","ES","GO","MA","MT","MS",
            "MG","PA","PB","PR","PE","PI","RJ","RN","RS","RO","RR","SC",
            "SP","SE","TO"
        };

        private static readonly Regex CroRegex = new Regex(
            @"^([A-Z]{2})[\s\-]?(\d{4,6})$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public ValidationResult Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ValidationResult.Failure("O CRO é obrigatório.");

            var match = CroRegex.Match(value.Trim());
            if (!match.Success)
                return ValidationResult.Failure("CRO inválido. Use o formato: UF-XXXXXX (ex.: SP-123456).");

            string uf = match.Groups[1].Value.ToUpperInvariant();
            if (!ValidUFs.Contains(uf))
                return ValidationResult.Failure($"UF inválida no CRO: {uf}.");

            return ValidationResult.Success();
        }

        #region 7.1
        //Formata para UF-XXXXXX (ex.: SP-123456). Use no onValueChanged do input//
        public static string FormatInput(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            var match = Regex.Match(value.Trim(), @"^([A-Za-z]{2})[\s\-]?(\d{4,6})$");
            if (!match.Success) return value;
            return $"{match.Groups[1].Value.ToUpperInvariant()}-{match.Groups[2].Value}";
        }
        #endregion
    }

    #endregion


    
    #region 8. 
    // Mesma estrutura do CRO: UF seguida de 4 a 6 dígitos//
    public class CrmValidator : IFieldValidator
    {
        public RegistrationFieldType FieldType => RegistrationFieldType.CRM;

        private static readonly string[] ValidUFs =
        {
            "AC","AL","AP","AM","BA","CE","DF","ES","GO","MA","MT","MS",
            "MG","PA","PB","PR","PE","PI","RJ","RN","RS","RO","RR","SC",
            "SP","SE","TO"
        };

        private static readonly Regex CrmRegex = new Regex(
            @"^([A-Z]{2})[\s\-]?(\d{4,6})$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public ValidationResult Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ValidationResult.Failure("O CRM é obrigatório.");

            var match = CrmRegex.Match(value.Trim());
            if (!match.Success)
                return ValidationResult.Failure("CRM inválido. Use o formato: UF-XXXXXX (ex.: SP-123456).");

            string uf = match.Groups[1].Value.ToUpperInvariant();
            if (!ValidUFs.Contains(uf))
                return ValidationResult.Failure($"UF inválida no CRM: {uf}.");

            return ValidationResult.Success();
        }

        #region 8.1 
        //Formata para UF-XXXXXX (ex.: SP-123456). Use no onValueChanged do input//
        public static string FormatInput(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            var match = Regex.Match(value.Trim(), @"^([A-Za-z]{2})[\s\-]?(\d{4,6})$");
            if (!match.Success) return value;
            return $"{match.Groups[1].Value.ToUpperInvariant()}-{match.Groups[2].Value}";
        }
        #endregion
    }

    #region 9.
    // ---------- Validação de senha ------ //
    public class PasswordValidator : IFieldValidator
    {
        public RegistrationFieldType FieldType => RegistrationFieldType.Password;
        public ValidationResult Validate(string value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return ValidationResult.Failure("A senha é obrigatória.");
            }
            return ValidationResult.Success();
        }
        
        #endregion
        #region 9.1
        //Formatação de senha para ********//
        public static string FormatInput(string value)
        {
            if(string.IsNullOrWhiteSpace(value)) return value;
            return new string('*', value.Length);
        
        }
    }

    #endregion

     #region 10.
    // -------------- RESULTADO DA VALIDAÇÃO -------------------//
    // Struct imutável que representa o resultado de qualquer validação de campo.
    // IsValid         → true se o campo passou na validação.
    // Message         → mensagem de erro quando IsValid é false, vazio quando é true.
    // FailedFieldType → qual campo falhou (preenchido quando IsValid é false); use para limpar o input.
    public readonly struct ValidationResult
    {
        public bool IsValid { get; }
        public string Message { get; }
        /// <summary>Campo que falhou na validação (preenchido quando IsValid é false). Use para limpar o input correspondente.</summary>
        public RegistrationFieldType? FailedFieldType { get; }

        public ValidationResult(bool isValid, string message = null, RegistrationFieldType? failedFieldType = null)
        {
            IsValid = isValid;
            Message = message ?? string.Empty;
            FailedFieldType = failedFieldType;
        }

        public static ValidationResult Success() => new ValidationResult(true);
        public static ValidationResult Failure(string message, RegistrationFieldType? failedFieldType = null) => new ValidationResult(false, message, failedFieldType);
    }
    #endregion
}

#endregion