using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Mono.Data.Sqlite;

namespace Playout.Registration
{
    // ==================================================================================
    //  SAVE DATA MANAGER
    //  Responsável por persistir os dados do formulário de cadastro.
    //  Suporta: JSON, SQLite ou ambos, conforme o SaveMode do RegistrationConfig.
    // ==================================================================================

    #region Indice
    // ========================= INDICE DE FUNCIONAMENTO ========================= //
    //
    // 1. ------ Constantes e construtor (config, nomes de arquivo, pasta de corrompidos) ------ //
    //
    // 2. ------ Save (método principal — decide qual formato usar) ------ //
    //
    // 3. ------ Salvamento em JSON ------ //
    // 3.1 ------ Verificação de integridade JSON (tenta principal → tenta cópia) ------ //
    // 3.2 ------ Leitura de um único arquivo JSON ------ //
    // 3.3 ------ RegistrationListWrapper (estrutura do JSON) ------ //
    //
    // 4. ------ Salvamento em SQLite ------ //
    // 4.1 ------ Verificação de integridade SQLite (PRAGMA integrity_check) ------ //
    // 4.2 ------ Validação de arquivo SQLite (SqliteFileIsValid) ------ //
    // 4.3 ------ Criação da tabela (CREATE TABLE IF NOT EXISTS) ------ //
    // 4.4 ------ Inserção de registro (INSERT com parâmetros) ------ //
    //
    // 5. ------ Proteção contra corrupção ------ //
    // 5.1 ------ Mover arquivo corrompido para pasta de backup (CorruptedBackups) ------ //
    // 5.2 ------ Cópia de segurança (TryCopyFile — redundância entre principal e cópia) ------ //
    //
    // 6. ------ Métodos auxiliares (caminhos dos arquivos e pastas) ------ //
    #endregion

    public class SaveDataManager
    {
        #region 1.
        // ========================= CONSTANTES E CONSTRUTOR ========================= //
        // _config       → referência ao ScriptableObject RegistrationConfig (mesmo que o da validação).
        // Os arquivos de persistência ficam em Application.persistentDataPath:
        //   - registrations.json / registrations_backup.json  (JSON principal e cópia)
        //   - registrations.db   / registrations_backup.db    (SQLite principal e cópia)
        // CorruptedBackups → subpasta para onde arquivos corrompidos são MOVIDOS (nunca apagados).

        private readonly RegistrationConfig _config;

        private const string JsonFileName = "registrations.json";
        private const string JsonBackupFileName = "registrations_backup.json";
        private const string SqliteFileName = "registrations.db";
        private const string SqliteBackupFileName = "registrations_backup.db";
        private const string CorruptedBackupsFolder = "CorruptedBackups";

        // Recebe o RegistrationConfig para ler o SaveMode configurado no Inspector.
        // Quem instancia o SaveDataManager deve passar o mesmo config usado na validação.
        public SaveDataManager(RegistrationConfig config)
        {
            _config = config;
        }
        #endregion


        #region 2.
        // ========================= SAVE (MÉTODO PRINCIPAL) ========================= //
        // Lê o SaveMode do RegistrationConfig e direciona para o(s) método(s) correto(s).
        // Retorna true se salvou com sucesso em todos os formatos solicitados.
        // Se o modo for NaoSalvar, retorna true sem fazer nada.
        //
        // Fluxo:
        //   1) Converte RegistrationFormData → SerializableFormData (campos públicos para JsonUtility).
        //   2) Se SaveMode inclui JSON  → chama SaveToJson.
        //   3) Se SaveMode inclui SQLite → chama SaveToSqlite.
        //   4) Retorna true se todos os saves solicitados tiveram sucesso.

        /// <summary>
        /// Salva os dados do formulário conforme o SaveMode configurado no RegistrationConfig.
        /// Chame após a validação ter passado com sucesso.
        /// </summary>
        public bool Save(RegistrationFormData formData)
        {
            if (_config == null)
            {
                Debug.LogError("[SaveDataManager] RegistrationConfig é null. Não é possível salvar.");
                return false;
            }

            SaveMode mode = _config.SaveMode;

            if (mode == SaveMode.NaoSalvar)
            {
                Debug.Log("[SaveDataManager] SaveMode = NaoSalvar. Nenhum dado foi persistido.");
                return true;
            }

            SerializableFormData data = formData.ToSerializable();
            bool success = true;

            if (mode == SaveMode.SalvarJson || mode == SaveMode.SalvarEmJson_E_SQLite)
            {
                success &= SaveToJson(data);
            }

            if (mode == SaveMode.SalvarSQLite || mode == SaveMode.SalvarEmJson_E_SQLite)
            {
                success &= SaveToSqlite(data);
            }

            return success;
        }
        #endregion


        #region 3.
        // ========================= SALVAMENTO EM JSON ========================= //
        // O arquivo JSON armazena uma LISTA de registros (histórico do evento).
        // Cada chamada de Save carrega a lista existente, adiciona o novo registro e reescreve.
        //
        // Redundância: DOIS arquivos são escritos com o mesmo conteúdo (principal + cópia).
        // Se o principal corromper, a cópia pode ser usada na próxima leitura.
        //
        // Cada registro recebe um ID incremental simples (último ID + 1, ou 1 se lista vazia).
        //
        // Caminho: Application.persistentDataPath/registrations.json
        // Exemplo:
        //   { "registros": [ { "id":1, "name":"João", "email":"joao@ex.com", ... }, ... ] }

        private bool SaveToJson(SerializableFormData data)
        {
            try
            {
                string mainPath = GetJsonPath();
                string backupPath = GetJsonBackupPath();

                // 3.1 — Verifica integridade e carrega a lista (principal ou cópia)
                RegistrationListWrapper wrapper = TryLoadJsonOrCreateNew(mainPath, backupPath);

                // Gera o próximo ID incremental
                int nextId = wrapper.registros.Count > 0
                    ? wrapper.registros[wrapper.registros.Count - 1].id + 1
                    : 1;
                data.id = nextId;

                wrapper.registros.Add(data);

                // Reescreve os DOIS arquivos com a mesma lista (redundância)
                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(mainPath, json);
                File.WriteAllText(backupPath, json);

                Debug.Log($"[SaveDataManager] Dados salvos em JSON (principal e cópia): {mainPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveDataManager] Erro ao salvar em JSON: {ex.Message}");
                return false;
            }
        }

        #region 3.1
        // ------ Verificação de integridade JSON (tenta principal → tenta cópia) ------ //
        //
        // Ordem de tentativa:
        //   1) Tenta ler o arquivo PRINCIPAL (registrations.json).
        //      → Se válido: usa ele.
        //   2) Se principal está corrompido/vazio/inexistente:
        //      → Move o principal para a pasta CorruptedBackups (ver seção 5.1).
        //      → Tenta ler a CÓPIA (registrations_backup.json).
        //      → Se a cópia é válida: usa ela e loga que o principal foi recuperado.
        //   3) Se a cópia TAMBÉM está corrompida:
        //      → Move a cópia para CorruptedBackups.
        //      → Retorna um wrapper vazio (lista nova, sem registros).
        //      → Loga que ambos estavam corrompidos e um novo será criado.

        private RegistrationListWrapper TryLoadJsonOrCreateNew(string mainPath, string backupPath)
        {
            // Tenta o arquivo principal
            var wrapper = TryLoadSingleJson(mainPath);
            if (wrapper != null)
                return wrapper;

            // Principal inválido → move para pasta de corrompidos e tenta a cópia
            TryMoveCorruptedToBackupFolder(mainPath);
            wrapper = TryLoadSingleJson(backupPath);
            if (wrapper != null)
            {
                Debug.Log("[SaveDataManager] Arquivo principal JSON corrompido. Dados carregados da cópia de segurança.");
                return wrapper;
            }

            // Cópia também inválida → move para corrompidos e cria um novo wrapper vazio
            TryMoveCorruptedToBackupFolder(backupPath);
            Debug.Log("[SaveDataManager] Arquivos JSON corrompidos ou inexistentes. Criando um novo agora.");
            return new RegistrationListWrapper();
        }
        #endregion

        #region 3.2
        // ------ Leitura de um único arquivo JSON ------ //
        //
        // Tenta ler e deserializar um arquivo JSON.
        // Retorna o RegistrationListWrapper se o arquivo existir e for válido.
        // Retorna null se:
        //   - O arquivo não existir.
        //   - O conteúdo estiver vazio ou em branco.
        //   - O JsonUtility não conseguir deserializar (formato inválido / corrompido).
        //   - O wrapper ou a lista interna forem null (estrutura inesperada).

        private static RegistrationListWrapper TryLoadSingleJson(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                string existingJson = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(existingJson))
                    return null;

                var wrapper = JsonUtility.FromJson<RegistrationListWrapper>(existingJson);
                if (wrapper == null || wrapper.registros == null)
                    return null;

                return wrapper;
            }
            catch
            {
                return null;
            }
        }
        #endregion

        #region 3.3
        // ------ RegistrationListWrapper (estrutura do JSON) ------ //
        //
        // Wrapper necessário porque JsonUtility não serializa List<T> diretamente na raiz.
        // JsonUtility exige um objeto (classe com [Serializable]) que CONTENHA a lista.
        // A estrutura no arquivo fica: { "registros": [ {...}, {...}, ... ] }

        [Serializable]
        private class RegistrationListWrapper
        {
            public List<SerializableFormData> registros = new List<SerializableFormData>();
        }
        #endregion
        #endregion


        #region 4.
        // ========================= SALVAMENTO EM SQLITE ========================= //
        // Cria (se não existir) um banco SQLite com a tabela "registrations"
        // e insere os dados do formulário como uma nova linha.
        //
        // Redundância: após gravar no banco principal, copia o arquivo .db inteiro
        // para a cópia de segurança (registrations_backup.db).
        //
        // A tabela possui: id (auto-incremento), uma coluna por campo e created_at (timestamp).
        // Caminho: Application.persistentDataPath/registrations.db

        private bool SaveToSqlite(SerializableFormData data)
        {
            try
            {
                string dbPath = GetSqlitePath();
                string backupDbPath = GetSqliteBackupPath();

                // 4.1 — Verifica integridade do banco principal (e restaura da cópia se necessário)
                EnsureValidSqliteFile(dbPath, backupDbPath);

                string connectionString = $"URI=file:{dbPath}";

                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();

                    // 4.3 — Cria a tabela se não existir
                    CreateTableIfNeeded(connection);

                    // 4.4 — Insere o registro
                    InsertRecord(connection, data);
                }

                // 5.2 — Atualiza a cópia de segurança com o conteúdo do banco principal
                TryCopyFile(dbPath, backupDbPath);

                Debug.Log($"[SaveDataManager] Dados salvos em SQLite (principal e cópia): {dbPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveDataManager] Erro ao salvar em SQLite: {ex.Message}");
                return false;
            }
        }

        #region 4.1
        // ------ Verificação de integridade SQLite ------ //
        //
        // Antes de qualquer gravação, verifica se o banco principal está íntegro.
        // Usa PRAGMA integrity_check (ver 4.2) para validar a estrutura interna do SQLite.
        //
        // Fluxo:
        //   1) Se o .db principal não existir → não faz nada (será criado na conexão).
        //   2) Se o .db principal está íntegro (integrity_check = "ok") → continua normalmente.
        //   3) Se o .db principal está CORROMPIDO:
        //      → Move o corrompido para CorruptedBackups (ver 5.1).
        //      → Se a CÓPIA (.db backup) existir e estiver íntegra:
        //          → Copia a cópia de volta para o caminho do principal (restauração).
        //          → Loga: "Banco principal restaurado a partir da cópia de segurança."
        //      → Se a cópia TAMBÉM estiver corrompida:
        //          → Move a cópia para CorruptedBackups.
        //          → Loga: "Cópia SQLite também corrompida. Será criado um novo banco."
        //          → O próximo connection.Open() criará um banco .db vazio novo.

        private void EnsureValidSqliteFile(string dbPath, string backupDbPath)
        {
            if (!File.Exists(dbPath))
                return;

            // Banco principal íntegro → nada a fazer
            if (SqliteFileIsValid(dbPath))
                return;

            // Banco principal corrompido → move para pasta de corrompidos
            Debug.Log("[SaveDataManager] Arquivo SQLite principal corrompido. Movendo para pasta de backup e tentando usar a cópia.");
            TryMoveCorruptedToBackupFolder(dbPath);

            // Tenta restaurar a partir da cópia
            if (File.Exists(backupDbPath) && SqliteFileIsValid(backupDbPath))
            {
                try
                {
                    File.Copy(backupDbPath, dbPath, overwrite: true);
                    Debug.Log("[SaveDataManager] Banco principal restaurado a partir da cópia de segurança.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveDataManager] Não foi possível restaurar principal a partir da cópia: {ex.Message}");
                }
            }
            else
            {
                // Cópia também corrompida ou inexistente → move e loga
                if (File.Exists(backupDbPath))
                {
                    TryMoveCorruptedToBackupFolder(backupDbPath);
                    Debug.Log("[SaveDataManager] Cópia SQLite também corrompida. Será criado um novo banco.");
                }
            }
        }
        #endregion

        #region 4.2
        // ------ Validação de arquivo SQLite (SqliteFileIsValid) ------ //
        //
        // Abre o banco e executa: PRAGMA integrity_check;
        // Esse comando percorre a estrutura interna do SQLite e retorna:
        //   - "ok"   → banco está íntegro.
        //   - outra string ou exceção → banco corrompido ou impossível de abrir.
        //
        // Retorna true apenas se integrity_check retornar "ok".
        // Se não conseguir nem abrir a conexão (arquivo truncado, binário inválido, etc.), retorna false.

        private static bool SqliteFileIsValid(string dbPath)
        {
            try
            {
                string connectionString = $"URI=file:{dbPath}";
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();
                    using (var cmd = new SqliteCommand("PRAGMA integrity_check;", connection))
                    {
                        object result = cmd.ExecuteScalar();
                        string value = result?.ToString() ?? "";
                        return string.Equals(value, "ok", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region 4.3
        // ------ Criação da tabela (CREATE TABLE IF NOT EXISTS) ------ //
        //
        // Executado toda vez que o banco é aberto (idempotente graças ao IF NOT EXISTS).
        // Colunas:
        //   id         → INTEGER PRIMARY KEY AUTOINCREMENT (ID único gerado pelo SQLite).
        //   name … password → TEXT (cada campo do formulário).
        //   created_at → TEXT com data/hora de inserção (preenchido automaticamente pelo SQLite).

        private void CreateTableIfNeeded(SqliteConnection connection)
        {
            string sql = @"
                CREATE TABLE IF NOT EXISTS registrations (
                    id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    name        TEXT,
                    last_name   TEXT,
                    email       TEXT,
                    phone       TEXT,
                    cpf         TEXT,
                    cnpj        TEXT,
                    rg          TEXT,
                    cro         TEXT,
                    crm         TEXT,
                    password    TEXT,
                    created_at  TEXT DEFAULT (datetime('now','localtime'))
                );";

            using (var cmd = new SqliteCommand(sql, connection))
            {
                cmd.ExecuteNonQuery();
            }
        }
        #endregion

        #region 4.4
        // ------ Inserção de registro (INSERT com parâmetros) ------ //
        //
        // Insere uma nova linha na tabela registrations com os dados do formulário.
        // Usa parâmetros (@param) em vez de concatenar strings no SQL → previne SQL Injection.
        // O campo id é gerado automaticamente pelo AUTOINCREMENT do SQLite.
        // O campo created_at é preenchido automaticamente pelo DEFAULT da coluna.

        private void InsertRecord(SqliteConnection connection, SerializableFormData data)
        {
            string sql = @"
                INSERT INTO registrations (name, last_name, email, phone, cpf, cnpj, rg, cro, crm, password)
                VALUES (@name, @lastName, @email, @phone, @cpf, @cnpj, @rg, @cro, @crm, @password);";

            using (var cmd = new SqliteCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@name", data.name);
                cmd.Parameters.AddWithValue("@lastName", data.lastName);
                cmd.Parameters.AddWithValue("@email", data.email);
                cmd.Parameters.AddWithValue("@phone", data.phone);
                cmd.Parameters.AddWithValue("@cpf", data.cpf);
                cmd.Parameters.AddWithValue("@cnpj", data.cnpj);
                cmd.Parameters.AddWithValue("@rg", data.rg);
                cmd.Parameters.AddWithValue("@cro", data.cro);
                cmd.Parameters.AddWithValue("@crm", data.crm);
                cmd.Parameters.AddWithValue("@password", data.password);

                cmd.ExecuteNonQuery();
            }
        }
        #endregion
        #endregion


        #region 5.
        // ========================= PROTEÇÃO CONTRA CORRUPÇÃO ========================= //
        //
        // Estratégia de proteção (dois mecanismos):
        //   A) Redundância: cada formato (JSON ou SQLite) mantém DOIS arquivos idênticos
        //      (principal + cópia). Se o principal corromper, a cópia é usada.
        //   B) Preservação: arquivos corrompidos NUNCA são apagados. São MOVIDOS para a
        //      pasta CorruptedBackups com timestamp no nome, para análise ou recuperação manual.

        #region 5.1
        // ------ Mover arquivo corrompido para pasta de backup (CorruptedBackups) ------ //
        //
        // Quando um arquivo (JSON ou .db) é considerado corrompido, ele é movido para:
        //   Application.persistentDataPath/CorruptedBackups/
        //
        // O nome do arquivo movido recebe um sufixo com data/hora para nunca sobrescrever:
        //   Exemplo: registrations_corrompido_2026-03-04_14-30-55.json
        //
        // Se a pasta CorruptedBackups não existir, ela é criada automaticamente.
        // Se não for possível mover (ex: permissão negada), apenas loga um warning.

        private void TryMoveCorruptedToBackupFolder(string path)
        {
            if (!File.Exists(path))
                return;

            try
            {
                // Garante que a pasta de corrompidos existe
                string folder = GetCorruptedBackupsPath();
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                // Monta o nome de destino: nomeOriginal_corrompido_AAAA-MM-DD_HH-MM-SS.extensão
                string fileName = Path.GetFileNameWithoutExtension(path);
                string extension = Path.GetExtension(path);
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string destPath = Path.Combine(folder, $"{fileName}_corrompido_{timestamp}{extension}");

                // Move o arquivo corrompido (não copia — o original sai do caminho original)
                File.Move(path, destPath);
                Debug.Log($"[SaveDataManager] Arquivo corrompido movido para backup (não perdido): {destPath}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveDataManager] Não foi possível mover arquivo corrompido para pasta de backup: {ex.Message}");
            }
        }
        #endregion

        #region 5.2
        // ------ Cópia de segurança (TryCopyFile) ------ //
        //
        // Após cada gravação bem-sucedida no arquivo/banco principal, copia o arquivo
        // inteiro para o caminho da cópia de segurança (sobrescreve a cópia anterior).
        // Assim a cópia está sempre sincronizada com o principal.
        //
        // Usado em dois momentos:
        //   - JSON: ambos os arquivos são escritos com File.WriteAllText na mesma operação.
        //   - SQLite: após o INSERT, o .db principal é copiado com File.Copy para o backup.
        //
        // Se a cópia falhar (ex: disco cheio), apenas loga um warning — o principal já foi salvo.

        private static void TryCopyFile(string sourcePath, string destPath)
        {
            try
            {
                if (File.Exists(sourcePath))
                    File.Copy(sourcePath, destPath, overwrite: true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveDataManager] Não foi possível atualizar cópia de segurança: {ex.Message}");
            }
        }
        #endregion
        #endregion


        #region 6.
        // ========================= MÉTODOS AUXILIARES (CAMINHOS) ========================= //
        //
        // Estrutura de arquivos gerada:
        //   persistentDataPath/
        //   ├── registrations.json            ← JSON principal
        //   ├── registrations_backup.json     ← JSON cópia de segurança
        //   ├── registrations.db              ← SQLite principal
        //   ├── registrations_backup.db       ← SQLite cópia de segurança
        //   └── CorruptedBackups/             ← pasta com arquivos corrompidos (preservados)
        //       ├── registrations_corrompido_2026-03-04_14-30-55.json
        //       └── registrations_corrompido_2026-03-04_14-30-55.db

        /// <summary>Caminho completo do arquivo JSON de registros (principal).</summary>
        public string GetJsonPath() => Path.Combine(Application.persistentDataPath, JsonFileName);

        /// <summary>Caminho completo da cópia de segurança do JSON.</summary>
        public string GetJsonBackupPath() => Path.Combine(Application.persistentDataPath, JsonBackupFileName);

        /// <summary>Caminho completo do banco SQLite de registros (principal).</summary>
        public string GetSqlitePath() => Path.Combine(Application.persistentDataPath, SqliteFileName);

        /// <summary>Caminho completo da cópia de segurança do banco SQLite.</summary>
        public string GetSqliteBackupPath() => Path.Combine(Application.persistentDataPath, SqliteBackupFileName);

        /// <summary>Pasta para onde arquivos corrompidos são movidos (nunca apagados).</summary>
        public string GetCorruptedBackupsPath() => Path.Combine(Application.persistentDataPath, CorruptedBackupsFolder);
        #endregion
    }
}
