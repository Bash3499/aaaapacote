using UnityEngine;

namespace Playout.Registration
{
    // ----------------------------------------------------------------------------------
    //  Modo de persistência dos dados de cadastro.
    // ----------------------------------------------------------------------------------

    
    /// Define onde os dados do formulário de cadastro devem ser salvos.
    public enum SaveMode
    {
        [Tooltip("Não persistir os dados em nenhum formato")]
        NaoSalvar = 0,

        [Tooltip("Salvar apenas em arquivo JSON")]
        SalvarJson = 1,

        [Tooltip("Salvar apenas em SQLite")]
        SalvarSQLite = 2,

        [Tooltip("Salvar em JSON e em SQLite")]
        SalvarEmJson_E_SQLite = 3
    }

    // ----------------------------------------------------------------------------------
    //  Estruturas que agrupam os campos no Inspector (cada uma vira uma seção recolhível).
    // ----------------------------------------------------------------------------------

    [System.Serializable]
    public struct PersonalDataFields
    {
        [Tooltip("Exibir e validar campo Nome")]
        public bool name;
        [Tooltip("Exibir e validar campo Sobrenome")]
        public bool lastName;
    }

    [System.Serializable]
    public struct ContactFields
    {
        [Tooltip("Exibir e validar campo E-mail")]
        public bool email;
        [Tooltip("Exibir e validar campo Telefone")]
        public bool phone;
    }

    [System.Serializable]
    public struct DocumentFields
    {
        [Tooltip("Exibir e validar campo CPF")]
        public bool cpf;
        [Tooltip("Exibir e validar campo CNPJ")]
        public bool cnpj;
        [Tooltip("Exibir e validar campo RG")]
        public bool rg;
    }

    [System.Serializable]
    public struct ProfessionalFields
    {
        [Tooltip("Exibir e validar campo CRO")]
        public bool cro;
        [Tooltip("Exibir e validar campo CRM")]
        public bool crm;
    }

    [System.Serializable]
    public struct SecurityFields
    {
        [Tooltip("Exibir e validar campo Senha")]
        public bool password;
    }

    // ----------------------------------------------------------------------------------
    //  ScriptableObject de configuração do cadastro.
    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Configuração do cadastro. Cada checkbox ativa o campo no formulário e sua validação.
    /// Crie via: Create → Playout → Registration Config
    /// </summary>
    [CreateAssetMenu(fileName = "RegistrationConfig", menuName = "Playout/Registration Config")]
    public class RegistrationConfig : ScriptableObject
    {
        [Header("Persistência")]
        [Tooltip("Onde e como os dados do cadastro devem ser salvos.")]
        [SerializeField] private SaveMode saveMode = SaveMode.NaoSalvar;

        [Header("Dados pessoais")]
        [SerializeField] private PersonalDataFields personal = new PersonalDataFields
        {
            name = true,
            lastName = true
        };

        [Header("Contato")]
        [SerializeField] private ContactFields contact = new ContactFields
        {
            email = false,
            phone = false
        };

        [Header("Documentos")]
        [SerializeField] private DocumentFields documents = new DocumentFields
        {
            cpf = false,
            cnpj = false,
            rg = false
        };

        [Header("Registro profissional")]
        [SerializeField] private ProfessionalFields professional = new ProfessionalFields
        {
            cro = false,
            crm = false
        };

        [Header("Segurança")]
        [SerializeField] private SecurityFields security = new SecurityFields
        {
            password = false
        };

        // --- Propriedades públicas ---

        /// <summary>Modo de persistência configurado (não salvar, JSON, SQLite ou ambos).</summary>
        public SaveMode SaveMode => saveMode;

        public bool InsertName => personal.name;
        public bool InsertLastName => personal.lastName;
        public bool InsertEmail => contact.email;
        public bool InsertPhone => contact.phone;
        public bool InsertCPF => documents.cpf;
        public bool InsertCNPJ => documents.cnpj;
        public bool InsertRG => documents.rg;
        public bool InsertCRO => professional.cro;
        public bool InsertCRM => professional.crm;
        public bool InsertPassword => security.password;

        /// <summary>
        /// Indica se o campo do tipo informado está ativo (checkbox marcada).
        /// Quando ativo, o campo deve ser exibido e validado.
        /// </summary>
        public bool IsFieldEnabled(RegistrationFieldType fieldType)
        {
            return fieldType switch
            {
                RegistrationFieldType.Name => personal.name,
                RegistrationFieldType.LastName => personal.lastName,
                RegistrationFieldType.Email => contact.email,
                RegistrationFieldType.Phone => contact.phone,
                RegistrationFieldType.CPF => documents.cpf,
                RegistrationFieldType.CNPJ => documents.cnpj,
                RegistrationFieldType.RG => documents.rg,
                RegistrationFieldType.CRO => professional.cro,
                RegistrationFieldType.CRM => professional.crm,
                RegistrationFieldType.Password => security.password,
                _ => false
            };
        }
    }
}
