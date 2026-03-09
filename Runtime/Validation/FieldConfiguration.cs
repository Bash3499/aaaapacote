using System.Collections.Generic;

namespace Playout.Registration
{
    // ==================================================================================
    //  FIELD CONFIGURATION
    //  Concentra: tipos de campo, contrato de validação e orquestrador de validação.
    // ==================================================================================

    #region Indice
    // ========================= INDICE DE COMENTÁRIOS ========================= //
    //
    // 1. ------ Enum usado para identificar a existência de um campo para a validação ------ //
    //
    // 2. ------ Interface: tem o campo declarado para a validação ------ //
    //
    // 3. ------ Orquestrador de validação (RegistrationValidator) ------ //

    // 4. ------ Verificação de erros (ValidateAll) ------ //

    #endregion
    #region 1.
    // ------ Tipos de campo (RegistrationFieldType) ------ //
  // -------- Enum usado para identificar a existencia de um campo para a validação -------- //
    public enum RegistrationFieldType
    {
        Name,
        LastName,
        Email,
        Phone,
        CPF,
        CNPJ,
        RG,
        CRO,
        CRM,
        Password
    }

    #endregion
    #region 2.
    // ------ tem o campo declarado para a validação ------ //
    public interface IFieldValidator
    {
        RegistrationFieldType FieldType { get; }
        ValidationResult Validate(string value);
    }

    #endregion
    #region 3.
    //------ Orquestrador de validação (RegistrationValidator) ------ //
    
    //  e chama o validador correspondente apenas quando o campo está habilitado.
    //  - RegisterValidator(...) → registra/substitui um validador para um tipo de campo.
    //  - Validate(...)          → valida um único campo.
    //  - ValidateAll(...)       → valida todos os campos ativos de uma vez. ------ //

    public class RegistrationValidator
    {
        private readonly RegistrationConfig _config;
        private readonly Dictionary<RegistrationFieldType, IFieldValidator> _validators;

        public RegistrationValidator(RegistrationConfig config)
        {
            _config = config;
            _validators = new Dictionary<RegistrationFieldType, IFieldValidator>();
        }

        /// Registra um validador para um tipo de campo. Sobrescreve se já existir.
        public void RegisterValidator(IFieldValidator validator)
        {
            _validators[validator.FieldType] = validator;
        }

        /// Verifica se o campo está ativo (checkbox marcada) no config.
        public bool IsFieldEnabled(RegistrationFieldType fieldType) => _config.IsFieldEnabled(fieldType);

        /// Valida o valor de um campo. Campo desativado retorna sucesso automaticamente.
        /// Em caso de falha, o resultado inclui o campo que falhou (FailedFieldType) para permitir limpar o input.
        public ValidationResult Validate(RegistrationFieldType fieldType, string value)
        {
            if (!_config.IsFieldEnabled(fieldType))
                return ValidationResult.Success();

            if (!_validators.TryGetValue(fieldType, out var validator))
                return ValidationResult.Failure($"Validação não configurada para o campo: {fieldType}.", fieldType);

            var result = validator.Validate(value);
            if (!result.IsValid)
                return ValidationResult.Failure(result.Message, fieldType);
            return result;
        }
        
        #endregion
        #region 4.
        /// Valida todos os campos ativos. Retorna o primeiro erro encontrado (com FailedFieldType) ou sucesso.
        /// sempre para no primeiro erro encontrado, só continua a verificação de o campo for preenchido corretamente
        public ValidationResult ValidateAll(IReadOnlyDictionary<RegistrationFieldType, string> fieldValues)
        {
            if (fieldValues == null)
                return ValidationResult.Failure("Dados do formulário não informados.", null);

            foreach (var kv in fieldValues)
            {
                if (!_config.IsFieldEnabled(kv.Key))
                    continue;

                var result = Validate(kv.Key, kv.Value);
                if (!result.IsValid)
                    return result;
            }

            return ValidationResult.Success();
        }
    }
}
#endregion
