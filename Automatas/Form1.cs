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

        // NUEVA: tabla de funciones
        Dictionary<string, Funcion> tablaFunciones = new Dictionary<string, Funcion>();

        public Form1()
        {
            InitializeComponent();
            InicializarEditorCodigoFuente();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void InicializarEditorCodigoFuente()
        {
            scintilla1.StyleResetDefault();
            scintilla1.Styles[Style.Default].Font = "Consolas";
            scintilla1.Styles[Style.Default].Size = 10;
            scintilla1.Styles[Style.Default].ForeColor = Color.Black;
            scintilla1.Styles[Style.Default].BackColor = Color.LightGray;
            scintilla1.StyleClearAll();
            // === ACTIVAR NÚMEROS DE LÍNEA ===
            scintilla1.Margins[0].Width = 40;
            scintilla1.Margins[0].Type = MarginType.Number;
            scintilla1.Margins[0].Sensitive = false;
            scintilla1.Margins[0].Mask = 0;

            scintilla2.StyleResetDefault();
            scintilla2.Styles[Style.Default].Font = "Consolas";
            scintilla2.Styles[Style.Default].Size = 10;
            scintilla2.Styles[Style.Default].ForeColor = Color.Black;
            scintilla2.Styles[Style.Default].BackColor = Color.Gray;
            scintilla2.StyleClearAll();
            scintilla2.Margins[0].Width = 40;
            scintilla2.Margins[0].Type = MarginType.Number;
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
            scintilla1.Styles[5].ForeColor = Color.Yellow;       // Cadenas
            scintilla1.Styles[6].ForeColor = Color.Coral;        // Caracteres
            scintilla1.Styles[7].ForeColor = Color.GreenYellow;  // Comentarios
            scintilla1.Styles[8].ForeColor = Color.DarkGoldenrod;// Caracteres especiales
            scintilla1.Styles[9].ForeColor = Color.Red;          // Errores
            scintilla1.Styles[10].ForeColor = Color.Black;       // ASIG

            // === INDICADOR PARA LÍNEAS CON ERROR ===
            var indError = scintilla1.Indicators[0];
            indError.Style = IndicatorStyle.StraightBox;
            indError.ForeColor = Color.Red;
            indError.Alpha = 40;
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
            scintilla1.IndicatorCurrent = 0;
            scintilla1.IndicatorClearRange(0, scintilla1.TextLength);

            foreach (var err in listaErrores)
            {
                int lineaIndex = err.linea - 1;
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
            int linea = scintilla1.CurrentLine;
            int start = scintilla1.Lines[linea].Position;
            int length = scintilla1.Lines[linea].Length;

            scintilla1.StartStyling(start);
            scintilla1.SetStyling(length, 0);

            ReAnalizarLinea(linea);
        }

        private void ReAnalizarLinea(int linea)
        {
            string texto = scintilla1.Lines[linea].Text;

            tokensGlobales.RemoveAll(t => t.linea == linea);

            List<(int linea, int columna, string token)> tokens = TokenizarLinea(texto, linea);

            foreach (var tk in tokens)
            {
                string token = tk.token;
                int columna = tk.columna;
                token = token.Trim();
                string resultado = lexico.AnalizarCadena(token);
                Console.WriteLine($"|{token}| → {resultado}");

                int estilo = ObtenerEstilo(resultado);

                tokensGlobales.Add((linea, columna, token, estilo));

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
            return 9;
        }

        private void Scintilla1_StyleNeeded(object sender, StyleNeededEventArgs e)
        {
            foreach (var tk in tokensGlobales)
            {
                PintarToken(tk.linea, tk.columna, tk.token.Length, tk.estilo);
            }
        }


        private void PintarToken(int linea, int columnaInicio, int longitud, int estilo)
        {
            try
            {
                int pos = scintilla1.Lines[linea].Position + columnaInicio;
                scintilla1.StartStyling(pos);
                scintilla1.SetStyling(longitud, estilo);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        //=========================================== Editor de Tokens =================================================

        private void NuevaLineaTokens()
        {
            scintilla2.AppendText("\n");
        }

        private void AgregarTokenATabla(string token)
        {
            string t = token.Replace("TOKEN: ", "");

            if (scintilla2.Lines.Count == 1 && scintilla2.Lines[0].Text == "")
            {
                scintilla2.AppendText(t);
                return;
            }

            int ultima = scintilla2.Lines.Count - 1;
            string textoUltima = scintilla2.Lines[ultima].Text.Trim();

            if (textoUltima.Length == 0)
            {
                scintilla2.AppendText(t);
            }
            else
            {
                scintilla2.AppendText(" " + t);
            }
        }


        private List<(int linea, int columna, string token)> TokenizarLinea(string linea, int numLinea)
        {
            List<(int linea, int columna, string token)> tokens = new List<(int linea, int columna, string token)>();

            int j = 0;

            while (j < linea.Length)
            {
                if (char.IsWhiteSpace(linea[j]))
                {
                    j++;
                    continue;
                }

                if (linea[j] == '"')
                {
                    int inicio = j;
                    j++;

                    while (j < linea.Length && linea[j] != '"')
                        j++;

                    if (j < linea.Length && linea[j] == '"')
                    {
                        j++;
                    }
                    else
                    {
                        j = linea.Length;
                    }

                    string tok = linea.Substring(inicio, j - inicio);
                    tokens.Add((numLinea, inicio, tok));
                    continue;
                }

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
            dgvErrores.Rows.Clear();

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

        // NUEVO: tabla de funciones
        private void LlenarTablaFunciones()
        {
            dgvFunciones.Rows.Clear();

            foreach (var f in tablaFunciones.Values)
            {
                string listaParams = string.Join(", ", f.Parametros.Select(p => $"{p.tipo} {p.nombre}"));

                string cuerpo = "";
                if (f.LineaCuerpoInicio > 0 && f.LineaCuerpoFin > 0)
                    cuerpo = $"{f.LineaCuerpoInicio} - {f.LineaCuerpoFin}";
                else
                    cuerpo = "—";

                dgvFunciones.Rows.Add(f.Nombre, f.TipoRetorno, listaParams, cuerpo);
            }
        }



        private int RegistrarIdentificador(string nombre, string tipo = "", string valor = "")
        {
            string id = nombre.Trim();

            if (tablaSimbolos.ContainsKey(id))
            {
                var s = tablaSimbolos[id];

                if (!string.IsNullOrEmpty(tipo))
                    s.Tipo = tipo;

                if (!string.IsNullOrEmpty(valor))
                    s.Valor = valor;

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
            if (!tablaFunciones.ContainsKey(nombreFuncion))
                return;

            var f = tablaFunciones[nombreFuncion];
            f.Parametros.Add((tipoParam, nombreParam));
        }

        // Mapea PR23..PR28 a tipo de dato de tu lenguaje
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

        // Obtiene el valor de una variable en una línea: vX = ... ;
        private string ObtenerValorAsignadoEnLinea(string lineaTexto, int columnaId)
        {
            int idxAsig = lineaTexto.IndexOf('=', columnaId);
            if (idxAsig == -1)
                return "";

            int idxPuntoComa = lineaTexto.IndexOf(';', idxAsig);
            if (idxPuntoComa == -1)
                idxPuntoComa = lineaTexto.Length;

            string valor = lineaTexto.Substring(idxAsig + 1, idxPuntoComa - idxAsig - 1);
            return valor.Trim();
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

                tokensGlobales.Clear();
                ReAnalizarTodo();
            }
        }

        private void btnEjecutar_Click(object sender, EventArgs e)
        {
            tokensGlobales.Clear();
            listaErrores.Clear();
            tablaSimbolos.Clear();
            tablaFunciones.Clear();
            contadorSimbolos = 1;
            scintilla2.Text = "";
            intTotalErrores = 0;

            int totalLineas = scintilla1.Lines.Count;

            string nombreFuncionActual = null;   // función que estamos procesando
            bool esDeclaracionFuncion = false;   // estamos dentro del encabezado FUNC
            bool dentroDeFuncion = false;        // estamos dentro del cuerpo { }
            string tipoActual = "";              // tipo actual leído (TXT, ENT, BOOL, etc.)

            for (int i = 0; i < totalLineas; i++)
            {
                string linea = scintilla1.Lines[i].Text;
                List<(int linea, int columna, string token)> tokens = new List<(int, int, string)>();

                // TOKENIZAR
                int j = 0;
                while (j < linea.Length)
                {
                    if (char.IsWhiteSpace(linea[j]))
                    {
                        j++;
                        continue;
                    }

                    if (linea[j] == '"')
                    {
                        int inicio = j;
                        j++;

                        while (j < linea.Length && linea[j] != '"')
                            j++;

                        if (j < linea.Length && linea[j] == '"')
                        {
                            j++;
                            string tok = linea.Substring(inicio, j - inicio).Trim();
                            tokens.Add((i + 1, inicio, tok));
                        }
                        else
                        {
                            string tok = linea.Substring(inicio).Trim();
                            tokens.Add((i + 1, inicio, tok));
                        }

                        continue;
                    }

                    int inicioToken = j;
                    while (j < linea.Length && !char.IsWhiteSpace(linea[j]))
                        j++;

                    string tokNormal = linea.Substring(inicioToken, j - inicioToken).Trim();
                    if (tokNormal.Length > 0)
                        tokens.Add((i + 1, inicioToken, tokNormal));
                }

                // PROCESAR TOKENS
                foreach (var tk in tokens)
                {
                    int lineaToken = tk.linea;
                    int columna = tk.columna;
                    string lexema = tk.token.Trim();

                    try
                    {
                        string resultado = lexico.AnalizarCadena(lexema);
                        string resultadoNormalizado = resultado.Replace("TOKEN: ", "");

                        if (resultadoNormalizado.ToLower().Contains("inválido") ||
                            resultadoNormalizado.ToLower().Contains("inválida") ||
                            resultadoNormalizado.ToLower().Contains("error"))
                        {
                            listaErrores.Add((lineaToken, resultadoNormalizado));
                            AgregarTokenATabla("error");
                            continue;
                        }

                        // FUNC
                        if (lexema.Equals("FUNC", StringComparison.OrdinalIgnoreCase))
                        {
                            esDeclaracionFuncion = true;
                            AgregarTokenATabla(resultadoNormalizado);
                            continue;
                        }

                        // TIPOS
                        if (resultadoNormalizado.StartsWith("PR"))
                        {
                            string posibleTipo = ObtenerTipoDesdePR(resultadoNormalizado);
                            if (!string.IsNullOrEmpty(posibleTipo))
                                tipoActual = posibleTipo;

                            AgregarTokenATabla(resultadoNormalizado);
                            continue;
                        }

                        // IDF = nombre de función
                        if (resultadoNormalizado.StartsWith("IDF"))
                        {
                            nombreFuncionActual = lexema;

                            tablaFunciones[nombreFuncionActual] = new Funcion()
                            {
                                Nombre = nombreFuncionActual,
                                TipoRetorno = tipoActual,
                                LineaInicio = lineaToken,
                                LineaCuerpoInicio = 0,
                                LineaCuerpoFin = 0
                            };

                            AgregarTokenATabla(resultadoNormalizado);
                            continue;
                        }

                        // PARÁMETROS DE FUNCIÓN
                        if (esDeclaracionFuncion && resultadoNormalizado.StartsWith("IDV"))
                        {
                            if (!string.IsNullOrEmpty(nombreFuncionActual))
                            {
                                string tipoParam = string.IsNullOrEmpty(tipoActual) ? "?" : tipoActual;
                                tablaFunciones[nombreFuncionActual].Parametros.Add((tipoParam, lexema));
                            }

                            AgregarTokenATabla(resultadoNormalizado);
                            continue;
                        }

                        // DECLARACIÓN DE VARIABLE GLOBAL (solo si NO estamos dentro de una función)
                        if (resultadoNormalizado.StartsWith("IDV") &&
                            !string.IsNullOrEmpty(tipoActual) &&
                            !dentroDeFuncion)
                        {
                            string valor = ObtenerValorAsignadoEnLinea(linea, columna);

                            int num = RegistrarIdentificador(lexema, tipoActual, valor);
                            AgregarTokenATabla(resultadoNormalizado + num.ToString());
                            continue;
                        }

                        // USO DE VARIABLE (NO declaración)
                        if (resultadoNormalizado.StartsWith("IDV"))
                        {
                            AgregarTokenATabla(resultadoNormalizado);
                            continue;
                        }

                        // Cualquier otro token
                        AgregarTokenATabla(resultadoNormalizado);
                    }
                    catch
                    {
                        listaErrores.Add((lineaToken, "ERROR_INTERNO"));
                        AgregarTokenATabla("ERROR_INTERNO");
                    }
                }

                // DETECTAR INICIO DEL CUERPO
                if (linea.Trim() == "{")
                {
                    dentroDeFuncion = true;

                    if (!string.IsNullOrEmpty(nombreFuncionActual))
                        tablaFunciones[nombreFuncionActual].LineaCuerpoInicio = i + 1;
                }

                // DETECTAR FIN DEL CUERPO
                if (linea.Trim() == "}")
                {
                    dentroDeFuncion = false;

                    if (!string.IsNullOrEmpty(nombreFuncionActual))
                        tablaFunciones[nombreFuncionActual].LineaCuerpoFin = i + 1;
                }

                // FIN DEL ENCABEZADO DE FUNCIÓN
                if (esDeclaracionFuncion && linea.Contains(")"))
                {
                    esDeclaracionFuncion = false;
                    tipoActual = "";
                }

                NuevaLineaTokens();

                intTotalErrores = listaErrores.Count;
                lblTotalErrores.Text = "Total de Errores: " + intTotalErrores.ToString();
                LlenarDgvErrores();
                LlenarTablaSimbolos();
                LlenarTablaFunciones();
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

        private void dgvFunciones_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        //============================================================================================================

    }
}
