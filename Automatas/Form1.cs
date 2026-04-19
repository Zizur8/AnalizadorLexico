using ScintillaNET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Automatas
{
    public partial class Form1 : Form
    {
        // --- Estado del editor y análisis ---
        private List<(int linea, int columna, string token, int estilo)> tokensGlobales
            = new List<(int linea, int columna, string token, int estilo)>();

        // Lista para colorear tokens en scintilla2 (línea índice 0-based)
        private List<(int linea, int columna, string token, int estilo)> tokensTokens2
            = new List<(int linea, int columna, string token, int estilo)>();

        private AnalizadorLexico lexico = new AnalizadorLexico("C:\\GatoSabeDB.sqlite");

        private List<(int linea, string mensaje)> listaErrores = new List<(int, string)>();
        private int intTotalErrores = 0;

        // Tabla de símbolos global
        private Dictionary<string, Simbolo> tablaSimbolos = new Dictionary<string, Simbolo>();
        private int contadorSimbolos = 1;

        // Tabla de funciones
        private Dictionary<string, Funcion> tablaFunciones = new Dictionary<string, Funcion>();

        public Form1()
        {
            InitializeComponent();
            InitializeEditors();
        }

        // -------------------- Inicialización --------------------

        private void InitializeEditors()
        {
            InitializeSourceEditor();
            InitializeTokensEditor();
        }

        private void InitializeSourceEditor()
        {
            scintilla1.StyleResetDefault();
            scintilla1.Styles[Style.Default].Font = "Consolas";
            scintilla1.Styles[Style.Default].Size = 10;
            scintilla1.Styles[Style.Default].ForeColor = Color.Black;
            scintilla1.Styles[Style.Default].BackColor = Color.LightGray;
            scintilla1.StyleClearAll();

            scintilla1.Margins[0].Width = 40;
            scintilla1.Margins[0].Type = MarginType.Number;
            scintilla1.Margins[0].Sensitive = false;
            scintilla1.Margins[0].Mask = 0;

            scintilla1.Lexer = Lexer.Container;
            scintilla1.StyleNeeded += Scintilla1_StyleNeeded;
            scintilla1.UpdateUI += Scintilla1_UpdateUI;

            // Estilos
            scintilla1.Styles[1].ForeColor = Color.Blue;         // PR
            scintilla1.Styles[2].ForeColor = Color.Black;        // ID
            scintilla1.Styles[3].ForeColor = Color.Orange;       // CNU
            scintilla1.Styles[4].ForeColor = Color.Purple;       // Operadores
            scintilla1.Styles[5].ForeColor = Color.Yellow;       // Cadenas
            scintilla1.Styles[6].ForeColor = Color.Coral;        // Caracteres
            scintilla1.Styles[7].ForeColor = Color.GreenYellow;  // Comentarios
            scintilla1.Styles[8].ForeColor = Color.DarkGoldenrod;// Caracteres especiales
            scintilla1.Styles[9].ForeColor = Color.Red;          // Errores
            scintilla1.Styles[10].ForeColor = Color.Black;       // ASIG

            // Indicador de línea con error
            var indError = scintilla1.Indicators[0];
            indError.Style = IndicatorStyle.StraightBox;
            indError.ForeColor = Color.Red;
            indError.Alpha = 40;
            indError.OutlineAlpha = 255;
        }

        private void InitializeTokensEditor()
        {
            scintilla2.StyleResetDefault();
            scintilla2.Styles[Style.Default].Font = "Consolas";
            scintilla2.Styles[Style.Default].Size = 10;
            scintilla2.Styles[Style.Default].ForeColor = Color.Black;
            scintilla2.Styles[Style.Default].BackColor = Color.LightGray;
            scintilla2.StyleClearAll();

            scintilla2.Margins[0].Width = 40;
            scintilla2.Margins[0].Type = MarginType.Number;
            scintilla2.Margins[0].Sensitive = false;
            scintilla2.Margins[0].Mask = 0;

            // Usar container para aplicar estilos manualmente
            scintilla2.Lexer = Lexer.Container;
            scintilla2.StyleNeeded += Scintilla2_StyleNeeded;

            // Definir los mismos colores que en scintilla1
            scintilla2.Styles[1].ForeColor = Color.Blue;         // PR
            scintilla2.Styles[2].ForeColor = Color.Black;        // ID
            scintilla2.Styles[3].ForeColor = Color.Orange;       // CNU
            scintilla2.Styles[4].ForeColor = Color.Purple;       // Operadores
            scintilla2.Styles[5].ForeColor = Color.Yellow;       // Cadenas
            scintilla2.Styles[6].ForeColor = Color.Coral;        // Caracteres
            scintilla2.Styles[7].ForeColor = Color.GreenYellow;  // Comentarios
            scintilla2.Styles[8].ForeColor = Color.DarkGoldenrod;// Caracteres especiales
            scintilla2.Styles[9].ForeColor = Color.Red;          // Errores
            scintilla2.Styles[10].ForeColor = Color.Black;       // ASIG
        }

        // -------------------- Reanálisis y resaltado --------------------

        private void ReAnalizarTodo()
        {
            tokensGlobales.Clear();
            for (int i = 0; i < scintilla1.Lines.Count; i++)
                ReAnalizarLinea(i);
        }

        private void ReAnalizarLinea(int linea)
        {
            string texto = scintilla1.Lines[linea].Text;
            tokensGlobales.RemoveAll(t => t.linea == linea);

            var tokens = TokenizarLinea(texto, linea);
            foreach (var tk in tokens)
            {
                string token = tk.token.Trim();
                string resultado = lexico.AnalizarCadena(token);
                int estilo = ObtenerEstilo(resultado);

                tokensGlobales.Add((linea, tk.columna, token, estilo));
                PintarToken(linea, tk.columna, token.Length, estilo);
            }
        }

        private void ResaltarLineasError()
        {
            scintilla1.IndicatorCurrent = 0;
            scintilla1.IndicatorClearRange(0, scintilla1.TextLength);

            foreach (var err in listaErrores)
            {
                int lineaIndex = err.linea - 1;
                if (lineaIndex < 0 || lineaIndex >= scintilla1.Lines.Count) continue;

                var linea = scintilla1.Lines[lineaIndex];
                scintilla1.IndicatorCurrent = 0;
                scintilla1.IndicatorFillRange(linea.Position, linea.Length);
            }
        }

        // -------------------- Eventos del editor --------------------

        private void Scintilla1_UpdateUI(object sender, UpdateUIEventArgs e)
        {
            int linea = scintilla1.CurrentLine;
            ReAnalizarLinea(linea);
        }

        private void scintilla1_TextChanged(object sender, EventArgs e)
        {
            int linea = scintilla1.CurrentLine;
            int start = scintilla1.Lines[linea].Position;
            int length = scintilla1.Lines[linea].Length;

            scintilla1.StartStyling(start);
            scintilla1.SetStyling(length, 0);

            ReAnalizarLinea(linea);
        }

        private void Scintilla1_StyleNeeded(object sender, StyleNeededEventArgs e)
        {
            foreach (var tk in tokensGlobales)
                PintarToken(tk.linea, tk.columna, tk.token.Length, tk.estilo);
        }

        private void Scintilla2_StyleNeeded(object sender, StyleNeededEventArgs e)
        {
            foreach (var tk in tokensTokens2)
                PintarTokenEnScintilla2(tk.linea, tk.columna, tk.token.Length, tk.estilo);
        }

        private void PintarToken(int linea, int columnaInicio, int longitud, int estilo)
        {
            try
            {
                int pos = scintilla1.Lines[linea].Position + columnaInicio;
                scintilla1.StartStyling(pos);
                scintilla1.SetStyling(longitud, estilo);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void PintarTokenEnScintilla2(int linea, int columnaInicio, int longitud, int estilo)
        {
            try
            {
                // Asegurarse que la línea existe
                if (linea < 0 || linea >= scintilla2.Lines.Count) return;
                int pos = scintilla2.Lines[linea].Position + columnaInicio;
                scintilla2.StartStyling(pos);
                scintilla2.SetStyling(longitud, estilo);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // -------------------- Tokenización --------------------

        private List<(int linea, int columna, string token)> TokenizarLinea(string linea, int numLinea)
        {
            var tokens = new List<(int linea, int columna, string token)>();
            int j = 0;

            while (j < linea.Length)
            {
                if (char.IsWhiteSpace(linea[j])) { j++; continue; }

                if (linea[j] == '"')
                {
                    int inicio = j++;
                    while (j < linea.Length && linea[j] != '"') j++;
                    if (j < linea.Length && linea[j] == '"') j++;
                    string tok = linea.Substring(inicio, j - inicio);
                    tokens.Add((numLinea, inicio, tok));
                    continue;
                }

                int inicioToken = j;
                while (j < linea.Length && !char.IsWhiteSpace(linea[j])) j++;
                string tokNormal = linea.Substring(inicioToken, j - inicioToken);
                tokens.Add((numLinea, inicioToken, tokNormal));
            }

            return tokens;
        }

        // -------------------- Utilidades de estilo / validación --------------------

        private bool IsTokenInvalido(string tokenResultado)
        {
            if (string.IsNullOrEmpty(tokenResultado)) return false;
            string lower = tokenResultado.ToLower();
            return lower.Contains("inválido") || lower.Contains("inválida") || lower.Contains("error");
        }

        private int ObtenerEstilo(string resultado)
        {
            if (IsTokenInvalido(resultado)) return 9;
            resultado = resultado.Replace("TOKEN: ", "");

            if (resultado.StartsWith("PR")) return 1;
            if (resultado.StartsWith("ID")) return 2;
            if (resultado.StartsWith("CN")) return 3;
            if (resultado.StartsWith("OP")) return 4;
            if (resultado.StartsWith("CAD")) return 5;
            if (resultado.StartsWith("CAR")) return 6;
            if (resultado.StartsWith("COM")) return 7;
            if (resultado.StartsWith("CE")) return 8;
            if (resultado.StartsWith("ASIG")) return 10;
            return 9;
        }

        // -------------------- Registro de símbolos y funciones --------------------

        private int RegistrarIdentificador(string nombre, string tipo = "", string valor = "")
        {
            string id = nombre.Trim();
            if (tablaSimbolos.ContainsKey(id))
            {
                var s = tablaSimbolos[id];
                if (!string.IsNullOrEmpty(tipo)) s.Tipo = tipo;
                if (!string.IsNullOrEmpty(valor)) s.Valor = valor;
                return s.Numero;
            }

            var sim = new Simbolo()
            {
                Numero = contadorSimbolos,
                Nombre = id,
                Tipo = tipo,
                Valor = valor
            };

            tablaSimbolos.Add(id, sim);
            contadorSimbolos++;
            return sim.Numero;
        }

        private void RegistrarFuncion(string nombre, string tipoRetorno, int lineaInicio)
        {
            string id = nombre.Trim();
            if (!tablaFunciones.ContainsKey(id))
            {
                var f = new Funcion()
                {
                    Nombre = id,
                    TipoRetorno = tipoRetorno,
                    LineaInicio = lineaInicio,
                    LineaCuerpoInicio = 0,
                    LineaCuerpoFin = 0
                };
                tablaFunciones.Add(id, f);
            }
        }

        private void AgregarParametroFuncion(string nombreFuncion, string tipoParam, string nombreParam)
        {
            if (!tablaFunciones.ContainsKey(nombreFuncion)) return;
            tablaFunciones[nombreFuncion].Parametros.Add((tipoParam, nombreParam));
        }

        // -------------------- Llenado de vistas (UI) --------------------

        private void LlenarDgvErrores()
        {
            dgvErrores.Rows.Clear();
            foreach (var err in listaErrores)
                dgvErrores.Rows.Add(err.linea, err.mensaje);
        }

        private void LlenarTablaSimbolos()
        {
            dgvSimbolos.Rows.Clear();
            foreach (var sim in tablaSimbolos.Values.OrderBy(s => s.Numero))
                dgvSimbolos.Rows.Add(sim.Numero, sim.Nombre, sim.Tipo, sim.Valor);
        }

        private void LlenarTablaFunciones()
        {
            dgvFunciones.Rows.Clear();
            foreach (var f in tablaFunciones.Values)
            {
                string listaParams = string.Join(", ", f.Parametros.Select(p => $"{p.tipo} {p.nombre}"));
                string cuerpo = (f.LineaCuerpoInicio > 0 && f.LineaCuerpoFin > 0)
                    ? $"{f.LineaCuerpoInicio} - {f.LineaCuerpoFin}"
                    : "—";
                dgvFunciones.Rows.Add(f.Nombre, f.TipoRetorno, listaParams, cuerpo);
            }
        }

        // -------------------- Extracción de valores en línea --------------------

        private string ObtenerValorAsignadoEnLinea(string lineaTexto, int columnaId)
        {
            int idxAsig = lineaTexto.IndexOf('=', columnaId);
            if (idxAsig == -1) return "";

            int idxPuntoComa = lineaTexto.IndexOf(';', idxAsig);
            if (idxPuntoComa == -1) idxPuntoComa = lineaTexto.Length;

            string valor = lineaTexto.Substring(idxAsig + 1, idxPuntoComa - idxAsig - 1);
            return valor.Trim();
        }

        // -------------------- Botones y acciones principales --------------------

        private void GuardarArchivo()
        {
            using (SaveFileDialog save = new SaveFileDialog())
            {
                save.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
                save.Title = "Guardar archivo";
                if (save.ShowDialog() == DialogResult.OK)
                {
                    System.IO.File.WriteAllText(save.FileName, scintilla1.Text);
                    MessageBox.Show("Archivo guardado correctamente.");
                }
            }
        }

        private void CargarArchivo()
        {
            using (OpenFileDialog open = new OpenFileDialog())
            {
                open.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
                open.Title = "Abrir archivo";
                if (open.ShowDialog() == DialogResult.OK)
                {
                    string contenido = System.IO.File.ReadAllText(open.FileName);
                    scintilla1.Text = contenido;
                    tokensGlobales.Clear();
                    ReAnalizarTodo();
                }
            }
        }

        // -------------------- Método principal de análisis (refactorizado) --------------------
        // Conserva la lógica original: tokeniza línea por línea, consulta el analizador léxico,
        // registra funciones y variables globales, detecta inicio/fin de cuerpos y errores.
        private void btnEjecutar_Click(object sender, EventArgs e)
        {
            // Reset estado
            tokensGlobales.Clear();
            tokensTokens2.Clear();
            listaErrores.Clear();
            tablaSimbolos.Clear();
            tablaFunciones.Clear();
            contadorSimbolos = 1;
            scintilla2.Text = "";
            intTotalErrores = 0;

            int totalLineas = scintilla1.Lines.Count;

            string nombreFuncionActual = null;
            bool esDeclaracionFuncion = false;
            bool dentroDeFuncion = false;
            string tipoActual = "";

            for (int i = 0; i < totalLineas; i++)
            {
                string linea = scintilla1.Lines[i].Text;
                var tokens = TokenizarLinea(linea, i + 1);

                foreach (var tk in tokens)
                {
                    int lineaToken = tk.linea;
                    int columna = tk.columna;
                    string lexema = tk.token.Trim();

                    try
                    {
                        string resultado = lexico.AnalizarCadena(lexema);
                        string resultadoNormalizado = resultado.Replace("TOKEN: ", "");
                        int estiloToken = ObtenerEstilo(resultado);

                        // Error léxico
                        if (IsTokenInvalido(resultado))
                        {
                            listaErrores.Add((lineaToken, resultadoNormalizado));
                            AgregarTokenATabla("error", 9);
                            continue;
                        }

                        // Inicio de declaración de función
                        if (lexema.Equals("FUNC", StringComparison.OrdinalIgnoreCase))
                        {
                            esDeclaracionFuncion = true;
                            AgregarTokenATabla(resultadoNormalizado, estiloToken);
                            continue;
                        }

                        // Tipos (PR23..PR28)
                        if (resultadoNormalizado.StartsWith("PR"))
                        {
                            string posibleTipo = ObtenerTipoDesdePR(resultadoNormalizado);
                            if (!string.IsNullOrEmpty(posibleTipo))
                                tipoActual = posibleTipo;

                            AgregarTokenATabla(resultadoNormalizado, estiloToken);
                            continue;
                        }

                        // Nombre de función (IDF)
                        if (resultadoNormalizado.StartsWith("IDF"))
                        {
                            nombreFuncionActual = lexema;
                            RegistrarFuncion(nombreFuncionActual, tipoActual, lineaToken);
                            AgregarTokenATabla(resultadoNormalizado, estiloToken);
                            continue;
                        }

                        // Parámetros dentro del encabezado de función
                        if (esDeclaracionFuncion && resultadoNormalizado.StartsWith("IDV"))
                        {
                            if (!string.IsNullOrEmpty(nombreFuncionActual))
                            {
                                string tipoParam = string.IsNullOrEmpty(tipoActual) ? "?" : tipoActual;
                                AgregarParametroFuncion(nombreFuncionActual, tipoParam, lexema);
                            }
                            AgregarTokenATabla(resultadoNormalizado, estiloToken);
                            continue;
                        }

                        // Declaración de variable global (solo si no estamos dentro de un cuerpo de función)
                        if (resultadoNormalizado.StartsWith("IDV") &&
                            !string.IsNullOrEmpty(tipoActual) &&
                            !dentroDeFuncion)
                        {
                            string valor = ObtenerValorAsignadoEnLinea(linea, columna);
                            int num = RegistrarIdentificador(lexema, tipoActual, valor);
                            AgregarTokenATabla(resultadoNormalizado + num.ToString(), estiloToken);
                            continue;
                        }

                        // Uso de variable (no declaración)
                        if (resultadoNormalizado.StartsWith("IDV"))
                        {
                            AgregarTokenATabla(resultadoNormalizado, estiloToken);
                            continue;
                        }

                        // Otros tokens
                        AgregarTokenATabla(resultadoNormalizado, estiloToken);
                    }
                    catch
                    {
                        listaErrores.Add((lineaToken, "ERROR_INTERNO"));
                        AgregarTokenATabla("ERROR_INTERNO", 9);
                    }
                }

                // Detectar inicio de cuerpo de función
                if (linea.Trim() == "{")
                {
                    dentroDeFuncion = true;
                    if (!string.IsNullOrEmpty(nombreFuncionActual))
                        tablaFunciones[nombreFuncionActual].LineaCuerpoInicio = i + 1;
                }

                // Detectar fin de cuerpo de función
                if (linea.Trim() == "}")
                {
                    dentroDeFuncion = false;
                    if (!string.IsNullOrEmpty(nombreFuncionActual))
                        tablaFunciones[nombreFuncionActual].LineaCuerpoFin = i + 1;
                }

                // Fin del encabezado de función (cerrado de paréntesis)
                if (esDeclaracionFuncion && linea.Contains(")"))
                {
                    esDeclaracionFuncion = false;
                    tipoActual = "";
                }

                NuevaLineaTokens();

                // Actualizar UI
                intTotalErrores = listaErrores.Count;
                lblTotalErrores.Text = "Total de Errores: " + intTotalErrores.ToString();
                LlenarDgvErrores();
                LlenarTablaSimbolos();
                LlenarTablaFunciones();
                ResaltarLineasError();
            }

            // Forzar repintado de scintilla2 para aplicar estilos recién añadidos
            scintilla2.Refresh();
        }

        // -------------------- Guardado de tokens --------------------

        private void GuardarTokens()
        {
            using (SaveFileDialog save = new SaveFileDialog())
            {
                save.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
                save.Title = "Guardar archivo de tokens";
                if (save.ShowDialog() == DialogResult.OK)
                {
                    System.IO.File.WriteAllText(save.FileName, scintilla2.Text);
                    MessageBox.Show("Tokens guardados correctamente.");
                }
            }
        }

        // -------------------- Eventos UI auxiliares --------------------

        private void btnGuardarPrograma_Click(object sender, EventArgs e) => GuardarArchivo();
        private void btnCargarPrograma_Click(object sender, EventArgs e) => CargarArchivo();
        private void btnGuardarArchivoTokens_Click(object sender, EventArgs e) => GuardarTokens();

        private void NuevaLineaTokens()
        {
            scintilla2.AppendText("\n");
        }

        /// <summary>
        /// Agrega un token a scintilla2 y registra su posición y estilo para coloreado.
        /// </summary>
        /// <param name="token">Texto del token (sin "TOKEN: ").</param>
        /// <param name="estilo">Índice de estilo (1..10). -1 = no colorear.</param>
        private void AgregarTokenATabla(string token, int estilo = -1)
        {
            string t = token.Replace("TOKEN: ", "");

            // Si la primera línea está vacía, la usamos
            if (scintilla2.Lines.Count == 1 && scintilla2.Lines[0].Text == "")
            {
                scintilla2.AppendText(t);
                if (estilo >= 0)
                {
                    // línea 0, columna 0
                    tokensTokens2.Add((0, 0, t, estilo));
                }
                return;
            }

            int ultima = scintilla2.Lines.Count - 1;
            string textoUltima = scintilla2.Lines[ultima].Text;
            int columnaInicio = (textoUltima.Length == 0) ? 0 : textoUltima.Length + 1; // +1 por el espacio

            // Añadir token con o sin espacio
            if (textoUltima.Trim().Length == 0)
            {
                scintilla2.AppendText(t);
            }
            else
            {
                scintilla2.AppendText(" " + t);
            }

            // Registrar token para coloreado (línea índice 'ultima')
            if (estilo >= 0)
            {
                tokensTokens2.Add((ultima, columnaInicio, t, estilo));
            }
        }

        // -------------------- Mapeo de PR a tipos --------------------

        private string ObtenerTipoDesdePR(string tokenPR)
        {
            switch (tokenPR)
            {
                case "PR23": return "ENT";
                case "PR24": return "DEC";
                case "PR25": return "TXT";
                case "PR26": return "BOOL";
                case "PR27": return "CAR";
                case "PR28": return "VAC";
                default: return "";
            }
        }

        // -------------------- Resto de eventos vacíos (UI) --------------------

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { }
        private void dgvFunciones_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
    }
}
