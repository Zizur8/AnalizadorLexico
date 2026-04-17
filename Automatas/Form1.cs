using ScintillaNET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Automatas
{
    public partial class Form1 : Form
    {
        List<(int linea, int columna, string token, int estilo)> tokensGlobales = new List<(int linea, int columna, string token, int estilo)>();
        AnalizadorLexico lexico = new AnalizadorLexico("C:\\GatoSabeDB.sqlite");

        List<(int linea, string mensaje)> listaErrores = new List<(int, string)>();
        int intTotalErrores = 0;

        Dictionary<string, Simbolo> tablaSimbolos = new Dictionary<string, Simbolo>();
        int contadorSimbolos = 1;


        public Form1()
        {
            InitializeComponent();
            InicializarEditorCodigoFuente();

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void InicializarEditorCodigoFuente() {
            scintilla1.StyleResetDefault();
            scintilla1.Styles[Style.Default].Font = "Consolas";
            scintilla1.Styles[Style.Default].Size = 12;
            scintilla1.Styles[Style.Default].ForeColor = Color.Black;
            scintilla1.Styles[Style.Default].BackColor = Color.LightGray;
            scintilla1.StyleClearAll();
            // === ACTIVAR NÚMEROS DE LÍNEA ===
            scintilla1.Margins[0].Width = 40; // ancho del margen
            scintilla1.Margins[0].Type = MarginType.Number; // tipo: números de línea
            scintilla1.Margins[0].Sensitive = false;
            scintilla1.Margins[0].Mask = 0;


            scintilla2.StyleResetDefault();
            scintilla2.Styles[Style.Default].Font = "Consolas";
            scintilla2.Styles[Style.Default].Size = 12;
            scintilla2.Styles[Style.Default].ForeColor = Color.Black;
            scintilla2.Styles[Style.Default].BackColor = Color.Gray;
            scintilla2.StyleClearAll();
            // === ACTIVAR NÚMEROS DE LÍNEA ===
            scintilla2.Margins[0].Width = 40; // ancho del margen
            scintilla2.Margins[0].Type = MarginType.Number; // tipo: números de línea
            scintilla2.Margins[0].Sensitive = false;
            scintilla2.Margins[0].Mask = 0;

            scintilla1.Lexer = Lexer.Container;
            scintilla1.StyleNeeded += Scintilla1_StyleNeeded;
            scintilla1.UpdateUI += Scintilla1_UpdateUI;


            // === ESTILOS DE TEXTO ===
            scintilla1.Styles[1].ForeColor = Color.Blue;         // PR
            scintilla1.Styles[2].ForeColor = Color.Black;        // ID
            scintilla1.Styles[3].ForeColor = Color.Orange;       // CNU
            scintilla1.Styles[4].ForeColor = Color.Purple;       // Operadores
            scintilla1.Styles[5].ForeColor = Color.Yellow;        // Cadenas
            scintilla1.Styles[6].ForeColor = Color.Coral;     // Caracteres
            scintilla1.Styles[7].ForeColor = Color.GreenYellow;     // Comentarios
            scintilla1.Styles[8].ForeColor = Color.DarkGoldenrod;        // Caracteres especiales
            scintilla1.Styles[9].ForeColor = Color.Red;          // Errores
            scintilla1.Styles[10].ForeColor = Color.Black;          // ASIG

            // === INDICADOR PARA LÍNEAS CON ERROR ===
            var indError = scintilla1.Indicators[0];
            indError.Style = IndicatorStyle.StraightBox;
            indError.ForeColor = Color.Red;
            indError.Alpha = 40;        // transparencia del relleno
            indError.OutlineAlpha = 255;

        }


        private void ReAnalizarTodo()
        {
            tokensGlobales.Clear();

            for (int i = 0; i < scintilla1.Lines.Count; i++)
            {
                ReAnalizarLinea(i);
            }
        }

        //=============================== Editor de codigo =============================================================

        private void ResaltarLineasError()
        {
            // Limpiar resaltados previos
            scintilla1.IndicatorCurrent = 0;
            scintilla1.IndicatorClearRange(0, scintilla1.TextLength);

            // Recorrer errores y pintar la línea completa
            foreach (var err in listaErrores)
            {
                int lineaIndex = err.linea - 1; // tus errores son 1-based, Scintilla es 0-based
                if (lineaIndex < 0 || lineaIndex >= scintilla1.Lines.Count)
                    continue;

                var linea = scintilla1.Lines[lineaIndex];
                int start = linea.Position;
                int length = linea.Length;

                scintilla1.IndicatorCurrent = 0;
                scintilla1.IndicatorFillRange(start, length);
            }
        }


        private void Scintilla1_UpdateUI(object sender, UpdateUIEventArgs e)
        {
            int linea = scintilla1.CurrentLine;
            ReAnalizarLinea(linea);
        }

        private void scintilla1_TextChanged(object sender, EventArgs e)
        {
            // 1. Limpiar estilos de la línea editada
            int linea = scintilla1.CurrentLine;
            int start = scintilla1.Lines[linea].Position;
            int length = scintilla1.Lines[linea].Length;

            scintilla1.StartStyling(start);
            scintilla1.SetStyling(length, 0); // estilo 0 = default

            // 2. Re-analizar SOLO esa línea
            ReAnalizarLinea(linea);

        }

        private void ReAnalizarLinea(int linea)
        {
            string texto = scintilla1.Lines[linea].Text;

            // 1. Borrar tokens previos de esa línea
            tokensGlobales.RemoveAll(t => t.linea == linea);

            // 2. Tokenizar igual que en tu botón
            List<(int linea, int columna, string token)> tokens = TokenizarLinea(texto, linea);

            // 3. Analizar cada token
            foreach (var tk in tokens)
            {
                string token = tk.token;
                int columna = tk.columna;
                token = token.Trim();
                string resultado = lexico.AnalizarCadena(token);
                Console.WriteLine($"|{token}| → {resultado}");

                int estilo = ObtenerEstilo(resultado);

                // Guardar para repintado
                tokensGlobales.Add((linea, columna, token, estilo));

                // Pintar inmediatamente
                PintarToken(linea, columna, token.Length, estilo);
            }
        }

        private int ObtenerEstilo(string resultado)
        {
            if (resultado.ToLower().Contains("inválido") ||
                resultado.ToLower().Contains("inválida") ||
                resultado.ToLower().Contains("error"))
            {
                return 9;
            }

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
            return 9; // error
        }

        private void Scintilla1_StyleNeeded(object sender, StyleNeededEventArgs e)
        {
            // Recorremos todos los tokens que ya analizaste
            foreach (var tk in tokensGlobales) // ← te explico esto abajo
            {
                PintarToken(tk.linea, tk.columna, tk.token.Length, tk.estilo);
            }
        }


        private void PintarToken(int linea, int columnaInicio, int longitud, int estilo)
        {
            int pos = scintilla1.Lines[linea].Position + columnaInicio;
            scintilla1.StartStyling(pos);
            scintilla1.SetStyling(longitud, estilo);
        }

        //=========================================== Editor de Tokens =================================================

        private void NuevaLineaTokens()
        {
            scintilla2.AppendText("\n");
        }

        private void AgregarTokenATabla(string token)
        {
            string t = token.Replace("TOKEN: ", "");

            // Si Scintilla2 está completamente vacío, agrega una línea inicial
            if (scintilla2.Lines.Count == 1 && scintilla2.Lines[0].Text == "")
            {
                scintilla2.AppendText(t);
                return;
            }

            // Obtener la última línea válida
            int ultima = scintilla2.Lines.Count - 1;
            string textoUltima = scintilla2.Lines[ultima].Text.Trim();

            // Si la última línea está vacía → agrega el token sin espacio
            if (textoUltima.Length == 0)
            {
                scintilla2.AppendText(t);
            }
            else
            {
                // Si ya hay algo → agrega espacio + token
                scintilla2.AppendText(" " + t);
            }
        }


        private List<(int linea, int columna, string token)> TokenizarLinea(string linea, int numLinea)
        {
            List<(int linea, int columna, string token)> tokens =
                new List<(int linea, int columna, string token)>();

            int j = 0;

            while (j < linea.Length)
            {
                // Saltar espacios
                if (char.IsWhiteSpace(linea[j]))
                {
                    j++;
                    continue;
                }



                // ⭐ 2. CADENA NORMAL (requiere cierre en la MISMA línea)
                if (linea[j] == '"')
                {
                    int inicio = j;
                    j++; // avanzar después de la primera comilla

                    // buscar cierre
                    while (j < linea.Length && linea[j] != '"')
                        j++;

                    if (j < linea.Length && linea[j] == '"')
                    {
                        j++; // incluir comilla final
                    }
                    else
                    {
                        // cadena sin cerrar → token inválido
                        j = linea.Length;
                    }

                    string tok = linea.Substring(inicio, j - inicio);
                    tokens.Add((numLinea, inicio, tok));
                    continue;
                }

                // ⭐ 3. TOKEN NORMAL
                int inicioToken = j;

                while (j < linea.Length && !char.IsWhiteSpace(linea[j]))
                    j++;

                string tokNormal = linea.Substring(inicioToken, j - inicioToken);
                tokens.Add((numLinea, inicioToken, tokNormal));
            }

            return tokens;
        }

        //=========================================== Tabla de Erroes ===============================================
        private void LlenarDgvErrores()
        {

            dgvErrores.Rows.Clear(); // limpiar tabla

            foreach (var err in listaErrores)
            {
                dgvErrores.Rows.Add(err.linea, err.mensaje);
            }

        }

        //=========================================== Tabla de simbolos =============================================

        private void LlenarTablaSimbolos()
        {
            dgvSimbolos.Rows.Clear();

            foreach (var sim in tablaSimbolos.Values.OrderBy(s => s.Numero))
            {
                dgvSimbolos.Rows.Add(sim.Numero, sim.Nombre, sim.Tipo, sim.Valor);
            }
        }

        private void RegistrarIdentificador(string nombre)
        {
            // Normalizar (opcional)
            string id = nombre.Trim();

            // ¿Ya existe?
            if (tablaSimbolos.ContainsKey(id))
                return; // evitar duplicados

            // Crear símbolo
            var sim = new Simbolo()
            {
                Numero = contadorSimbolos,
                Nombre = id
            };

            tablaSimbolos.Add(id, sim);
            contadorSimbolos++;
        }

        //===============================================  Botones  =====================================================================

        private void GuardarArchivo()
        {
            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
            save.Title = "Guardar archivo";

            if (save.ShowDialog() == DialogResult.OK)
            {
                System.IO.File.WriteAllText(save.FileName, scintilla1.Text);
                MessageBox.Show("Archivo guardado correctamente.");
            }
        }

        private void btnGuardarPrograma_Click(object sender, EventArgs e)
        {
            GuardarArchivo();
        }

        private void CargarArchivo()
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
            open.Title = "Abrir archivo";

            if (open.ShowDialog() == DialogResult.OK)
            {
                string contenido = System.IO.File.ReadAllText(open.FileName);
                scintilla1.Text = contenido;

                // Opcional: limpiar tokens y repintar
                tokensGlobales.Clear();
                ReAnalizarTodo();
            }
        }

        private void btnEjecutar_Click(object sender, EventArgs e)
        {
            // Resetear estructuras
            tokensGlobales.Clear();
            listaErrores.Clear();
            tablaSimbolos.Clear();
            contadorSimbolos = 1;
            scintilla2.Text = "";
            intTotalErrores = 0;



            int totalLineas = scintilla1.Lines.Count;

            // RECORRER LÍNEA POR LÍNEA
            for (int i = 0; i < totalLineas; i++)
            {
                string linea = scintilla1.Lines[i].Text;
                List<(int linea, int columna, string token)> tokens = new List<(int, int, string)>();


                int j = 0;
                while (j < linea.Length)
                {
                    // Saltar espacios
                    if (char.IsWhiteSpace(linea[j]))
                    {
                        j++;
                        continue;
                    }

                    // 1. CADENAS QUE EMPIEZAN CON "
                    if (linea[j] == '"')
                    {
                        int inicio = j;
                        j++; // avanzar después de la primera comilla

                        // Buscar la comilla de cierre
                        while (j < linea.Length && linea[j] != '"')
                            j++;

                        if (j < linea.Length && linea[j] == '"')
                        {
                            // CADENA COMPLETA
                            j++; // incluir la comilla final
                            string tok = linea.Substring(inicio, j - inicio).Trim();
                            tokens.Add((i + 1, inicio, tok));
                        }
                        else
                        {
                            // CADENA SIN CERRAR
                            string tok = linea.Substring(inicio).Trim();
                            tokens.Add((i + 1, inicio, tok));
                        }

                        continue;
                    }

                    // 2. TOKEN NORMAL (sin comillas)
                    int inicioToken = j;
                    while (j < linea.Length && !char.IsWhiteSpace(linea[j]))
                        j++;

                    string tokNormal = linea.Substring(inicioToken, j - inicioToken).Trim();
                    if (tokNormal.Length > 0)
                        tokens.Add((i + 1, inicioToken, tokNormal));
                }

                // ---- ANALIZAR TOKENS ----
                foreach (var tk in tokens)
                {
                    int lineaToken = tk.linea;
                    int columna = tk.columna;
                    string token = tk.token.Trim();

                    try
                    {
                        string resultado = lexico.AnalizarCadena(token);

                        Console.WriteLine($"|{token}| → {resultado}");

                        // Si el analizador detecta error
                        if (resultado.ToLower().Contains("inválido") ||
                            resultado.ToLower().Contains("inválida") ||
                            resultado.ToLower().Contains("error"))
                        {
                            listaErrores.Add((lineaToken, resultado));
                            AgregarTokenATabla("error");
                            continue;
                        }
                        else
                        {
                            RegistrarIdentificador(token);
                        }

                        // Agregar token al archivo de tokens
                        AgregarTokenATabla(resultado);

                    }
                    catch
                    {
                        listaErrores.Add((lineaToken, "ERROR_INTERNO"));
                        AgregarTokenATabla("ERROR_INTERNO");
                    }
                }

                // Nueva línea en archivo de tokens
                NuevaLineaTokens();

                // Actualizar UI
                intTotalErrores = listaErrores.Count;
                lblTotalErrores.Text = "Total de Errores: " + intTotalErrores.ToString();
                LlenarDgvErrores();
                LlenarTablaSimbolos();
                ResaltarLineasError();
            }
        }

        private void GuardarTokens()
        {
            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
            save.Title = "Guardar archivo de tokens";

            if (save.ShowDialog() == DialogResult.OK)
            {
                System.IO.File.WriteAllText(save.FileName, scintilla2.Text);
                MessageBox.Show("Tokens guardados correctamente.");
            }
        }

        
        private void btnCargarPrograma_Click(object sender, EventArgs e)
        {
            CargarArchivo();
        }

        private void btnGuardarArchivoTokens_Click(object sender, EventArgs e)
        {
            GuardarTokens();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        //============================================================================================================


    }
}
