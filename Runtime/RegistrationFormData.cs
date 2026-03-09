using System;
using System.Collections.Generic;

namespace Playout.Registration
{
    /// <summary>
    /// Modelo do formulário de cadastro. Cada propriedade é um campo ao qual você anexa o valor do input (UI).
    /// Preencha estas propriedades com os valores dos seus InputField/TMP_InputField e use ToFieldValues() para validar.
    /// </summary>
    public class RegistrationFormData
    {
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string CPF { get; set; }
        public string CNPJ { get; set; }
        public string RG { get; set; }
        public string CRO { get; set; }
        public string CRM { get; set; }
        public string Password { get; set; }

        /// <summary>
        /// Converte os campos para o formato esperado por RegistrationValidator.ValidateAll(...).
        /// Inclui apenas propriedades com valor não nulo (você pode trocar para incluir todos se preferir).
        /// </summary>
        public IReadOnlyDictionary<RegistrationFieldType, string> ToFieldValues()
        {
            var dict = new Dictionary<RegistrationFieldType, string>();

            if (Name != null) dict[RegistrationFieldType.Name] = Name;
            if (LastName != null) dict[RegistrationFieldType.LastName] = LastName;
            if (Email != null) dict[RegistrationFieldType.Email] = Email;
            if (Phone != null) dict[RegistrationFieldType.Phone] = Phone;
            if (CPF != null) dict[RegistrationFieldType.CPF] = CPF;
            if (CNPJ != null) dict[RegistrationFieldType.CNPJ] = CNPJ;
            if (RG != null) dict[RegistrationFieldType.RG] = RG;
            if (CRO != null) dict[RegistrationFieldType.CRO] = CRO;
            if (CRM != null) dict[RegistrationFieldType.CRM] = CRM;
            if (Password != null) dict[RegistrationFieldType.Password] = Password;

            return dict;
        }

        // Quando um campo não tiver sido preenchido, a propriedade é null; aqui ela vira "" (string vazia)
        // no dicionário. Assim o dicionário nunca tem valor null, evitando NullReferenceException ao ler.
        public IReadOnlyDictionary<RegistrationFieldType, string> ToFieldValuesIncludeEmpty()
        {
            return new Dictionary<RegistrationFieldType, string>
            {
                [RegistrationFieldType.Name] = Name ?? "",
                [RegistrationFieldType.LastName] = LastName ?? "",
                [RegistrationFieldType.Email] = Email ?? "",
                [RegistrationFieldType.Phone] = Phone ?? "",
                [RegistrationFieldType.CPF] = CPF ?? "",
                [RegistrationFieldType.CNPJ] = CNPJ ?? "",
                [RegistrationFieldType.RG] = RG ?? "",
                [RegistrationFieldType.CRO] = CRO ?? "",
                [RegistrationFieldType.CRM] = CRM ?? "",
                [RegistrationFieldType.Password] = Password ?? ""
            };
        }

        /// <summary>
        /// Converte para o formato serializável pelo JsonUtility (campos públicos).
        /// JsonUtility exige uma classe [Serializable] com campos públicos; RegistrationFormData
        /// usa properties, então precisamos dessa conversão.
        /// </summary>
        public SerializableFormData ToSerializable()
        {
            return new SerializableFormData
            {
                name = Name ?? "",
                lastName = LastName ?? "",
                email = Email ?? "",
                phone = Phone ?? "",
                cpf = CPF ?? "",
                cnpj = CNPJ ?? "",
                rg = RG ?? "",
                cro = CRO ?? "",
                crm = CRM ?? "",
                password = Password ?? ""
            };
        }
    }

    // ----------------------------------------------------------------------------------
    //  Versão serializável para JsonUtility e para a tabela SQLite.
    //  JsonUtility do Unity NÃO serializa properties (get/set), somente campos públicos
    //  em classes marcadas como [Serializable]. Por isso esta classe existe.
    // ----------------------------------------------------------------------------------

    [Serializable]
    public class SerializableFormData
    {
        public int id;
        public string name;
        public string lastName;
        public string email;
        public string phone;
        public string cpf;
        public string cnpj;
        public string rg;
        public string cro;
        public string crm;
        public string password;
    }
}
