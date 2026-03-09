using UnityEngine;
using TMPro;
using Playout.Registration;

/// <summary>
/// Controlador plug-and-play do formulário de cadastro Playout.
/// Arraste para o GameObject do formulário (ex.: CampoDeCadastro), atribua o RegistrationConfig e os campos no Inspector.
/// No botão de enviar, chame ValidateFields().
/// </summary>

    #region Indice
    // ========================= INDICE DE FUNCIONAMENTO ========================= //
    //
    // 1. ------ Configuração e referências (config, campos, feedback, flags) ------ //
    //
    // 2. ------ Inicialização (Awake - registro de validadores) ------ //
    //
    // 3. ------ Listeners (OnEnable / OnDisable) ------ //
    //
    // 4. ------ Formatadores em tempo real ------ //
    // 4.1 ------ ApplyFormatted (aplicar texto preservando cursor) ------ //
    //
    // 5. ------ Validação (ValidateFields, preenchimento do formulário) ------ //
    // 5.1 ------ GetInputText ------ //
    //
    // 6. ------ Tratamento de resultado (ClearField, ShowMessage) ------ //
    #endregion


    public class PackageManager : MonoBehaviour
    {
        // Atribua no Inspector: config, os campos de input e (opcional) o texto de feedback.
        // No botão de enviar, vincule a ValidateFields().

        #region 1.
        [Header("Configurações de Cadastro")]
        [SerializeField] private RegistrationConfig config;

        [Header("Campos de Input")]
        [SerializeField] private TMP_InputField nameField;
        [SerializeField] private TMP_InputField lastNameField;
        [SerializeField] private TMP_InputField emailField;
        [SerializeField] private TMP_InputField phoneField;
        [SerializeField] private TMP_InputField cpfField;
        [SerializeField] private TMP_InputField cnpjField;
        [SerializeField] private TMP_InputField rgField;
        [SerializeField] private TMP_InputField croField;
        [SerializeField] private TMP_InputField crmField;
        [SerializeField] private TMP_InputField passwordField;

        [Header("FeedBack de validação")]
        [Tooltip("Texto que será exibido quando a validação falhar ou funcionar")]
        [SerializeField] private TMP_Text messageFeedback;

        private RegistrationValidator validator;
        private SaveDataManager saveManager;
        private RegistrationFormData formData = new RegistrationFormData();

        private bool formattingName;
        private bool formattingLastName;
        private bool formattingPhone;
        private bool formattingCpf;
        private bool formattingCnpj;
        private bool formattingCro;
        private bool formattingCrm;
        private bool formattingPassword;

        private string realPassword = "";
    #endregion

    #region 2.
    private void Awake()
    {
        if(config == null) return;
        validator = new RegistrationValidator(config);
        saveManager = new SaveDataManager(config);
        validator.RegisterValidator(new NameValidator());
        validator.RegisterValidator(new LastNameValidator());
        validator.RegisterValidator(new EmailValidator());
        validator.RegisterValidator(new PhoneValidator());
        validator.RegisterValidator(new CpfValidator());
        validator.RegisterValidator(new CnpjValidator());
        validator.RegisterValidator(new RgValidator());
        validator.RegisterValidator(new CroValidator());
        validator.RegisterValidator(new CrmValidator());
        validator.RegisterValidator(new PasswordValidator());

        ApplyFieldVisibility();
    }

    /// <summary>
    /// Ativa ou desativa os GameObjects dos inputs conforme o RegistrationConfig.
    /// Campos desativados no ScriptableObject terão seus GameObjects desativados na tela.
    /// </summary>
    private void ApplyFieldVisibility()
    {
        if (config == null) return;

        SetFieldActive(nameField, config.IsFieldEnabled(RegistrationFieldType.Name));
        SetFieldActive(lastNameField, config.IsFieldEnabled(RegistrationFieldType.LastName));
        SetFieldActive(emailField, config.IsFieldEnabled(RegistrationFieldType.Email));
        SetFieldActive(phoneField, config.IsFieldEnabled(RegistrationFieldType.Phone));
        SetFieldActive(cpfField, config.IsFieldEnabled(RegistrationFieldType.CPF));
        SetFieldActive(cnpjField, config.IsFieldEnabled(RegistrationFieldType.CNPJ));
        SetFieldActive(rgField, config.IsFieldEnabled(RegistrationFieldType.RG));
        SetFieldActive(croField, config.IsFieldEnabled(RegistrationFieldType.CRO));
        SetFieldActive(crmField, config.IsFieldEnabled(RegistrationFieldType.CRM));
        SetFieldActive(passwordField, config.IsFieldEnabled(RegistrationFieldType.Password));
    }

    private static void SetFieldActive(TMP_InputField field, bool active)
    {
        if (field != null && field.gameObject != null)
            field.gameObject.SetActive(active);
    }
    #endregion

    #region 3.
    private void OnEnable()
    {
        if (nameField != null)     nameField.onValueChanged.AddListener(FormatNameField);
        if (lastNameField != null) lastNameField.onValueChanged.AddListener(FormatLastNameField);
        if (phoneField != null)    phoneField.onValueChanged.AddListener(FormatPhoneField);
        if (cpfField != null)      cpfField.onValueChanged.AddListener(FormatCpfField);
        if (cnpjField != null)     cnpjField.onValueChanged.AddListener(FormatCnpjField);
        if (croField != null)      croField.onValueChanged.AddListener(FormatCroField);
        if (crmField != null)      crmField.onValueChanged.AddListener(FormatCrmField);
        if (passwordField != null) passwordField.onValueChanged.AddListener(FormatPasswordField);
    }

    private void OnDisable()
    {
        if (nameField != null)     nameField.onValueChanged.RemoveListener(FormatNameField);
        if (lastNameField != null) lastNameField.onValueChanged.RemoveListener(FormatLastNameField);
        if (phoneField != null)    phoneField.onValueChanged.RemoveListener(FormatPhoneField);
        if (cpfField != null)      cpfField.onValueChanged.RemoveListener(FormatCpfField);
        if (cnpjField != null)     cnpjField.onValueChanged.RemoveListener(FormatCnpjField);
        if (croField != null)      croField.onValueChanged.RemoveListener(FormatCroField);
        if (crmField != null)      crmField.onValueChanged.RemoveListener(FormatCrmField);
        if (passwordField != null) passwordField.onValueChanged.RemoveListener(FormatPasswordField);
    }
    #endregion

    #region 4.
    private void FormatNameField(string value)
    {
        if (formattingName) return;
        string formatted = NameValidator.FormatInput(value);
        if (formatted == value) return;
        formattingName = true;
        ApplyFormatted(nameField, formatted);
        formattingName = false;
    }

    private void FormatLastNameField(string value)
    {
        if (formattingLastName) return;
        string formatted = LastNameValidator.FormatInput(value);
        if (formatted == value) return;
        formattingLastName = true;
        ApplyFormatted(lastNameField, formatted);
        formattingLastName = false;
    }

    private void FormatPhoneField(string value)
    {
        if (formattingPhone) return;
        string formatted = PhoneValidator.FormatInput(value);
        if (formatted == value) return;
        formattingPhone = true;
        ApplyFormatted(phoneField, formatted);
        formattingPhone = false;
    }

    private void FormatCpfField(string value)
    {
        if (formattingCpf) return;
        string formatted = CpfValidator.FormatInput(value);
        if (formatted == value) return;
        formattingCpf = true;
        ApplyFormatted(cpfField, formatted);
        formattingCpf = false;
    }

    private void FormatCnpjField(string value)
    {
        if (formattingCnpj) return;
        string formatted = CnpjValidator.FormatInput(value);
        if (formatted == value) return;
        formattingCnpj = true;
        ApplyFormatted(cnpjField, formatted);
        formattingCnpj = false;
    }

    private void FormatCroField(string value)
    {
        if (formattingCro) return;
        string formatted = CroValidator.FormatInput(value);
        if (formatted == value) return;
        formattingCro = true;
        ApplyFormatted(croField, formatted);
        formattingCro = false;
    }

    private void FormatCrmField(string value)
    {
        if (formattingCrm) return;
        string formatted = CrmValidator.FormatInput(value);
        if (formatted == value) return;
        formattingCrm = true;
        ApplyFormatted(crmField, formatted);
        formattingCrm = false;
    }

    private void FormatPasswordField(string value)
    {
        if (formattingPassword) return;
        string currentMask = new string('*', realPassword.Length);
        if (value.Length > currentMask.Length)
        {
            string newChars = value.Substring(currentMask.Length);
            realPassword += newChars;
        }
        else if (value.Length < currentMask.Length)
        {
            realPassword = realPassword.Substring(0, value.Length);
        }
        string formatted = new string('*', realPassword.Length);
        if (formatted == value) return;
        formattingPassword = true;
        ApplyFormatted(passwordField, formatted);
        formattingPassword = false;
    }

    private static void ApplyFormatted(TMP_InputField field, string formatted)
    {
        int pos = field.caretPosition;
        field.text = formatted;
        field.caretPosition = Mathf.Clamp(pos, 0, formatted.Length);
    }
    #endregion

    #region 5.
   public void ValidateFields()
   {
     if(validator == null)
     {
        ShowMessage("Configuração de validação não atribuida");
        return;
     }

        formData.Name = GetInputText(nameField);
        formData.LastName = GetInputText(lastNameField);
        formData.Email = GetInputText(emailField);
        formData.Phone = GetInputText(phoneField);
        formData.CPF = GetInputText(cpfField);
        formData.CNPJ = GetInputText(cnpjField);
        formData.RG = GetInputText(rgField);
        formData.CRO = GetInputText(croField);
        formData.CRM = GetInputText(crmField);
        formData.Password = realPassword;

   ValidationResult result = validator.ValidateAll(formData.ToFieldValuesIncludeEmpty());

   if(!result.IsValid)
   {
    ShowMessage(result.Message);
    ClearField(result.FailedFieldType);
    return;
   }

    if (saveManager != null && saveManager.Save(formData))
        ShowMessage("Formulário validado e dados salvos com sucesso!");
    else
        ShowMessage("Formulário validado, mas houve erro ao salvar os dados.");
   }

   private static string GetInputText(TMP_InputField field)
   {
    return field != null ? field.text : null;
   }
   #endregion

   #region 6.
   private void ClearField(RegistrationFieldType? failedFieldType)
   {
    if (!failedFieldType.HasValue) return;
    switch (failedFieldType.Value)
    {
     case RegistrationFieldType.Name:       if (nameField != null) nameField.text = ""; break;
     case RegistrationFieldType.LastName:     if (lastNameField != null) lastNameField.text = ""; break;
     case RegistrationFieldType.Email:        if (emailField != null) emailField.text = ""; break;
     case RegistrationFieldType.Phone:       if (phoneField != null) phoneField.text = ""; break;
     case RegistrationFieldType.CPF:         if (cpfField != null) cpfField.text = ""; break;
     case RegistrationFieldType.CNPJ:        if (cnpjField != null) cnpjField.text = ""; break;
     case RegistrationFieldType.RG:          if (rgField != null) rgField.text = ""; break;
     case RegistrationFieldType.CRO:         if (croField != null) croField.text = ""; break;
     case RegistrationFieldType.CRM:         if (crmField != null) crmField.text = ""; break;
     case RegistrationFieldType.Password:    if (passwordField != null) { passwordField.text = ""; realPassword = ""; } break;
    }
   }

   private void ShowMessage(string text)
   {
    if(messageFeedback != null)
        messageFeedback.text = text;
    else
        Debug.Log("[Playout Cadastro] " + text);
   }
   #endregion

}
