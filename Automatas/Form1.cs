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

        private List<(int linea, int columna, string token, int estilo)> tokensTokens2
            = new List<(int linea, int columna, string token, int estilo)>();

        private AnalizadorLexico lexico = new AnalizadorLexico("C:\\GatoSabeDB.sqlite");

        private List<(int linea, string mensaje)> listaErrores = new List<(int, string)>();
        private int intTotalErrores = 0;

        // --- Variables para el Análisis Sintáctico ---
        private List<(string Tipo, string Valor, int Linea)> tokensSintactico = new List<(string, string, int)>();
        private List<(int Linea, string Mensaje)> erroresSintacticos = new List<(int, string)>();
        private Dictionary<string, Dictionary<string, List<string>>> tablaM = new Dictionary<string, Dictionary<string, List<string>>>();

        // Tabla de símbolos y funciones
        private Dictionary<string, Simbolo> tablaSimbolos = new Dictionary<string, Simbolo>();
        private int contadorSimbolos = 1;
        private Dictionary<string, Funcion> tablaFunciones = new Dictionary<string, Funcion>();

        public Form1()
        {
            InitializeComponent();
            InitializeEditors();
            ConstruirTablaM(); // Inicializamos la Tabla LL1 completa
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

            scintilla1.TabWidth = 5;
            scintilla1.UseTabs = false;

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

            scintilla2.Lexer = Lexer.Container;
            scintilla2.StyleNeeded += Scintilla2_StyleNeeded;

            scintilla2.Styles[1].ForeColor = Color.Blue;
            scintilla2.Styles[2].ForeColor = Color.Black;
            scintilla2.Styles[3].ForeColor = Color.Orange;
            scintilla2.Styles[4].ForeColor = Color.Purple;
            scintilla2.Styles[5].ForeColor = Color.Yellow;
            scintilla2.Styles[6].ForeColor = Color.Coral;
            scintilla2.Styles[7].ForeColor = Color.GreenYellow;
            scintilla2.Styles[8].ForeColor = Color.DarkGoldenrod;
            scintilla2.Styles[9].ForeColor = Color.Red;
            scintilla2.Styles[10].ForeColor = Color.Black;
        }

        // -------------------- Inicialización de la Tabla M (Sintáctica COMPLETA) --------------------

        private void ConstruirTablaM()
        {
            tablaM.Clear();
            List<string> prTipos = new List<string> { "PR23", "PR24", "PR25", "PR26", "PR27" };

            // 1. Bloque Principal
            AgregarRegla("S", "PR1", "IN01");
            AgregarRegla("IN01", "PR1", "PR1", "CE3", "INS", "CE4");

            // INS -> IN INS | e
            AgregarRegla("INS", "CE4", "e");

            // FIRST(IN) para derivar INS -> IN INS
            foreach (var pr in prTipos) AgregarRegla("INS", pr, "IN", "INS");
            AgregarRegla("INS", "IDV", "IN", "INS");
            AgregarRegla("INS", "PR11", "IN", "INS");
            AgregarRegla("INS", "PR13", "IN", "INS");
            AgregarRegla("INS", "PR15", "IN", "INS");
            AgregarRegla("INS", "PR16", "IN", "INS");
            AgregarRegla("INS", "PR18", "IN", "INS");
            AgregarRegla("INS", "PR5", "IN", "INS");
            AgregarRegla("INS", "PR6", "IN", "INS");
            AgregarRegla("INS", "PR7", "IN", "INS");
            AgregarRegla("INS", "PR8", "IN", "INS");
            AgregarRegla("INS", "PR9", "IN", "INS");
            AgregarRegla("INS", "PR10", "IN", "INS");
            AgregarRegla("INS", "PR29", "IN", "INS");
            AgregarRegla("INS", "PR30", "IN", "INS");
            AgregarRegla("INS", "PR31", "IN", "INS");
            AgregarRegla("INS", "PR2", "IN", "INS");
            AgregarRegla("INS", "PR19", "IN", "INS");
            AgregarRegla("INS", "PR3", "IN", "INS");
            AgregarRegla("INS", "IDF", "IN", "INS");

            // 2. IN derivaciones
            foreach (var pr in prTipos) AgregarRegla("IN", pr, "IN02");
            AgregarRegla("IN", "IDV", "IN03");
            AgregarRegla("IN", "PR11", "IN04");
            AgregarRegla("IN", "PR13", "IN05");
            AgregarRegla("IN", "PR15", "IN06");
            AgregarRegla("IN", "PR16", "IN07");
            AgregarRegla("IN", "PR18", "IN08");
            AgregarRegla("IN", "PR5", "IN10");
            AgregarRegla("IN", "PR6", "IN11");
            AgregarRegla("IN", "PR7", "IN12");
            AgregarRegla("IN", "PR8", "IN13");
            AgregarRegla("IN", "PR9", "IN14");
            AgregarRegla("IN", "PR10", "IN15");
            AgregarRegla("IN", "PR29", "IN16");
            AgregarRegla("IN", "PR30", "IN17");
            AgregarRegla("IN", "PR31", "IN18");
            AgregarRegla("IN", "PR2", "IN19");
            AgregarRegla("IN", "PR19", "IN20");
            AgregarRegla("IN", "PR3", "IN21");
            AgregarRegla("IN", "IDF", "IN22");

            // 3. Declaraciones y Asignaciones
            foreach (var pr in prTipos) AgregarRegla("IN02", pr, "TIPO_VAR", "IDV", "ASIG_OPC", "CE8");
            foreach (var pr in prTipos) AgregarRegla("TIPO_VAR", pr, pr);

            AgregarRegla("ASIG_OPC", "ASIG", "ASIG", "EXP");
            AgregarRegla("ASIG_OPC", "CE8", "e");

            AgregarRegla("IN03", "IDV", "IDV", "ASIG", "EXP_IO", "CE8");
            AgregarRegla("EXP_IO", "PR4", "PR4", "CE1", "CE2");
            AgregarRegla("EXP_IO", "IDV", "EXP");
            AgregarRegla("EXP_IO", "CNU", "EXP");
            AgregarRegla("EXP_IO", "CAD", "EXP");
            AgregarRegla("EXP_IO", "CAR", "EXP");
            AgregarRegla("EXP_IO", "PR20", "EXP");
            AgregarRegla("EXP_IO", "PR21", "EXP");
            AgregarRegla("EXP_IO", "PR22", "EXP");
            AgregarRegla("EXP_IO", "IDF", "EXP");
            AgregarRegla("EXP_IO", "CE1", "EXP");

            // 4. Estructuras de Control
            AgregarRegla("IN04", "PR11", "PR11", "CE1", "COND", "CE2", "CE3", "INS", "CE4", "IN04_1");
            AgregarRegla("IN04_1", "PR12", "PR12", "CE3", "INS", "CE4");
            foreach (var t in prTipos) AgregarRegla("IN04_1", t, "e");
            AgregarRegla("IN04_1", "IDV", "e"); AgregarRegla("IN04_1", "PR11", "e");
            AgregarRegla("IN04_1", "CE4", "e"); AgregarRegla("IN04_1", "EOF", "e");

            AgregarRegla("IN05", "PR13", "PR13", "CE1", "IDV", "CE2", "CE3", "CASOS", "CE4");
            AgregarRegla("CASOS", "CNU", "VAL_CASO", "CE20", "INS", "PR19", "CE8", "CASOS");
            AgregarRegla("CASOS", "CAD", "VAL_CASO", "CE20", "INS", "PR19", "CE8", "CASOS");
            AgregarRegla("CASOS", "CAR", "VAL_CASO", "CE20", "INS", "PR19", "CE8", "CASOS");
            AgregarRegla("CASOS", "PR14", "PR14", "CE20", "INS", "PR19", "CE8");
            AgregarRegla("VAL_CASO", "CNU", "CNU");
            AgregarRegla("VAL_CASO", "CAD", "CAD");
            AgregarRegla("VAL_CASO", "CAR", "CAR");

            AgregarRegla("IN06", "PR15", "PR15", "CE1", "COND", "CE2", "CE3", "INS", "CE4");
            AgregarRegla("IN07", "PR16", "PR16", "CE3", "INS", "CE4", "PR17", "CE1", "COND", "CE2", "CE8");

            AgregarRegla("IN08", "PR18", "PR18", "CE1", "INIT_POR", "CE8", "COND", "CE8", "IDV", "ASIG", "EXP", "CE2", "CE3", "INS", "CE4");
            foreach (var pr in prTipos) AgregarRegla("INIT_POR", pr, "TIPO_VAR", "IDV", "ASIG", "EXP");
            AgregarRegla("INIT_POR", "IDV", "IDV", "ASIG", "EXP");

            AgregarRegla("IN20", "PR19", "PR19", "CE8");

            // 5. IO y Gráficas
            AgregarRegla("IN10", "PR5", "PR5", "CE1", "EXP", "CE2", "CE8");
            AgregarRegla("IN11", "PR6", "PR6", "CE1", "CE2", "CE8");
            AgregarRegla("IN12", "PR7", "PR7", "CE1", "CAD", "CE2", "CE8");
            AgregarRegla("IN13", "PR8", "PR8", "CE1", "CAD", "CE2", "CE8");
            AgregarRegla("IN14", "PR9", "PR9", "CE1", "CNU", "CE2", "CE8");
            AgregarRegla("IN15", "PR10", "PR10", "CE1", "CNU", "CE7", "CNU", "CE2", "CE8");
            AgregarRegla("IN16", "PR29", "PR29", "CE1", "TIPO_VAR", "CE7", "IDV", "CE7", "IDV", "CE2", "CE8");
            AgregarRegla("IN17", "PR30", "PR30", "CE1", "CE2", "CE8");
            AgregarRegla("IN18", "PR31", "PR31", "CE1", "CAD", "CE7", "CNU", "CE2", "CE8");

            // 6. Funciones
            AgregarRegla("IN19", "PR2", "PR2", "TIPO_RET", "IDF", "CE1", "PARAM", "CE2", "CE3", "INS", "CE4");
            foreach (var pr in prTipos) AgregarRegla("TIPO_RET", pr, "TIPO_VAR");
            AgregarRegla("TIPO_RET", "PR28", "PR28");

            foreach (var pr in prTipos) AgregarRegla("PARAM", pr, "TIPO_VAR", "IDV", "PARAM_P");
            AgregarRegla("PARAM", "CE2", "e");

            AgregarRegla("PARAM_P", "CE7", "CE7", "TIPO_VAR", "IDV", "PARAM_P");
            AgregarRegla("PARAM_P", "CE2", "e");

            AgregarRegla("IN21", "PR3", "PR3", "EXP_OPC", "CE8");

            List<string> firstExp = new List<string> { "IDV", "CNU", "CAD", "CAR", "PR20", "PR21", "PR22", "IDF", "CE1" };
            foreach (var f in firstExp) AgregarRegla("EXP_OPC", f, "EXP");
            AgregarRegla("EXP_OPC", "CE8", "e");

            AgregarRegla("IN22", "IDF", "CALL_FUNC", "CE8");
            AgregarRegla("CALL_FUNC", "IDF", "IDF", "CE1", "ARG", "CE2");
            foreach (var f in firstExp) AgregarRegla("ARG", f, "EXP", "ARG_P");
            AgregarRegla("ARG", "CE2", "e");
            AgregarRegla("ARG_P", "CE7", "CE7", "EXP", "ARG_P");
            AgregarRegla("ARG_P", "CE2", "e");

            // 8. Condiciones
            List<string> firstCond = new List<string> { "OPL3", "IDV", "CNU", "CAD", "CAR", "PR20", "PR21", "PR22", "IDF", "CE1" };
            foreach (var f in firstCond) AgregarRegla("COND", f, "COND_OR");
            foreach (var f in firstCond) AgregarRegla("COND_OR", f, "COND_AND", "COND_OR_P");

            AgregarRegla("COND_OR_P", "OPL2", "OPL2", "COND_AND", "COND_OR_P");
            AgregarRegla("COND_OR_P", "CE2", "e"); AgregarRegla("COND_OR_P", "CE8", "e");

            foreach (var f in firstCond) AgregarRegla("COND_AND", f, "COND_NOT", "COND_AND_P");

            AgregarRegla("COND_AND_P", "OPL1", "OPL1", "COND_NOT", "COND_AND_P");
            AgregarRegla("COND_AND_P", "OPL2", "e"); AgregarRegla("COND_AND_P", "CE2", "e"); AgregarRegla("COND_AND_P", "CE8", "e");

            AgregarRegla("COND_NOT", "OPL3", "OPL3", "COND_NOT");
            foreach (var f in firstExp) AgregarRegla("COND_NOT", f, "COND_REL");

            foreach (var f in firstExp) AgregarRegla("COND_REL", f, "EXP", "REL_OPC");

            List<string> opRels = new List<string> { "OPR1", "OPR2", "OPR3", "OPR4", "OPR5", "OPR6" };

            // CORRECCIÓN APLICADA: Directamente mapear 'r' en lugar del genérico 'OPR'
            foreach (var r in opRels) AgregarRegla("REL_OPC", r, r, "EXP");

            AgregarRegla("REL_OPC", "CE2", "e"); AgregarRegla("REL_OPC", "CE8", "e");
            AgregarRegla("REL_OPC", "OPL1", "e"); AgregarRegla("REL_OPC", "OPL2", "e");

            // 9. Expresiones aritméticas
            foreach (var f in firstExp) AgregarRegla("EXP", f, "TERM", "EXP_P");

            AgregarRegla("EXP_P", "OPA+", "OPA+", "TERM", "EXP_P");
            AgregarRegla("EXP_P", "OPA-", "OPA-", "TERM", "EXP_P");
            AgregarRegla("EXP_P", "CE2", "e"); AgregarRegla("EXP_P", "CE8", "e");
            AgregarRegla("EXP_P", "CE7", "e"); foreach (var r in opRels) AgregarRegla("EXP_P", r, "e");
            AgregarRegla("EXP_P", "OPL1", "e"); AgregarRegla("EXP_P", "OPL2", "e");

            foreach (var f in firstExp) AgregarRegla("TERM", f, "POT", "TERM_P");
            AgregarRegla("TERM_P", "OPA*", "OPA*", "POT", "TERM_P");
            AgregarRegla("TERM_P", "OPA/", "OPA/", "POT", "TERM_P");
            AgregarRegla("TERM_P", "OPA+", "e"); AgregarRegla("TERM_P", "OPA-", "e");
            AgregarRegla("TERM_P", "CE2", "e"); AgregarRegla("TERM_P", "CE8", "e");
            AgregarRegla("TERM_P", "CE7", "e"); foreach (var r in opRels) AgregarRegla("TERM_P", r, "e");
            AgregarRegla("TERM_P", "OPL1", "e"); AgregarRegla("TERM_P", "OPL2", "e");

            foreach (var f in firstExp) AgregarRegla("POT", f, "VALOR", "POT_P");
            AgregarRegla("POT_P", "OPA^", "OPA^", "POT");
            AgregarRegla("POT_P", "OPA*", "e"); AgregarRegla("POT_P", "OPA/", "e");
            AgregarRegla("POT_P", "OPA+", "e"); AgregarRegla("POT_P", "OPA-", "e");
            AgregarRegla("POT_P", "CE2", "e"); AgregarRegla("POT_P", "CE8", "e");
            AgregarRegla("POT_P", "CE7", "e"); foreach (var r in opRels) AgregarRegla("POT_P", r, "e");
            AgregarRegla("POT_P", "OPL1", "e"); AgregarRegla("POT_P", "OPL2", "e");

            AgregarRegla("VALOR", "IDV", "IDV");
            AgregarRegla("VALOR", "CNU", "CNU");
            AgregarRegla("VALOR", "CAD", "CAD");
            AgregarRegla("VALOR", "CAR", "CAR");
            AgregarRegla("VALOR", "PR20", "PR20");
            AgregarRegla("VALOR", "PR21", "PR21");
            AgregarRegla("VALOR", "PR22", "PR22");
            AgregarRegla("VALOR", "IDF", "CALL_FUNC");
            AgregarRegla("VALOR", "CE1", "CE1", "EXP", "CE2");
        }

        // Helper para llenar tablaM más limpio
        private void AgregarRegla(string noTerminal, string token, params string[] produccion)
        {
            if (!tablaM.ContainsKey(noTerminal)) tablaM[noTerminal] = new Dictionary<string, List<string>>();
            tablaM[noTerminal][token] = new List<string>(produccion);
        }

        // -------------------- Motor del Analizador Sintáctico LL1 --------------------

        private void EjecutarAnalisisSintactico()
        {
            Stack<string> pila = new Stack<string>();
            pila.Push("EOF");
            pila.Push("S"); // Símbolo Inicial

            tokensSintactico.Add(("EOF", "$", scintilla1.Lines.Count));
            int indice = 0;

            while (pila.Count > 0 && indice < tokensSintactico.Count)
            {
                string cima = pila.Peek();
                var tokenActual = tokensSintactico[indice];

                // Éxito total
                if (cima == "EOF" && tokenActual.Tipo == "EOF") break;

                if (EsTerminalSintactico(cima))
                {
                    if (cima == tokenActual.Tipo)
                    {
                        pila.Pop(); // Match 
                        indice++;
                    }
                    else
                    {
                        // TRADUCCIÓN DEL ERROR (Falta un terminal como ; o })
                        string esperado = TraducirToken(cima);
                        erroresSintacticos.Add((tokenActual.Linea, $"OMISIÓN: Falta {esperado} cerca de '{tokenActual.Valor}'"));

                        pila.Pop(); // RECUPERACIÓN MODO PÁNICO: Asumimos que se omitió y seguimos
                    }
                }
                else
                {
                    if (tablaM.ContainsKey(cima) && tablaM[cima].ContainsKey(tokenActual.Tipo))
                    {
                        pila.Pop();
                        var produccion = tablaM[cima][tokenActual.Tipo];

                        // Insertamos en la pila en orden inverso
                        for (int p = produccion.Count - 1; p >= 0; p--)
                        {
                            if (produccion[p] != "e") pila.Push(produccion[p]);
                        }
                    }
                    // ESTO EVITA LA CASCADA: Si no hay regla, pero el No Terminal se puede anular (epsilon)
                    // lo forzamos a anularse en lugar de crashear todo el bloque.
                    else if (tablaM.ContainsKey(cima) && tablaM[cima].Values.Any(prod => prod.Count == 1 && prod[0] == "e"))
                    {
                        // PÁNICO SUAVE: Forzamos la transición a epsilon y bajamos en la pila
                        pila.Pop();
                    }
                    else
                    {
                        // TRADUCCIÓN DEL ERROR (Estructura no reconocida)
                        string ayudaSintaxis = ObtenerAyudaSintaxis(cima);
                        erroresSintacticos.Add((tokenActual.Linea, $"SINTAXIS INVÁLIDA: Token inesperado '{tokenActual.Valor}'. {ayudaSintaxis}"));

                        // PÁNICO FUERTE (Sincronización): Saltamos el código dañado hasta el siguiente punto y coma ';'
                        while (indice < tokensSintactico.Count &&
                               tokensSintactico[indice].Tipo != "CE8" &&
                               tokensSintactico[indice].Tipo != "CE4" &&
                               tokensSintactico[indice].Tipo != "EOF")
                        {
                            indice++;
                        }

                        // Consumimos el punto y coma para arrancar la siguiente instrucción limpios
                        if (indice < tokensSintactico.Count && tokensSintactico[indice].Tipo == "CE8")
                        {
                            indice++;
                        }

                        // Limpiamos la pila de la instrucción matemática dañada, dejándola lista para la próxima instrucción
                        while (pila.Count > 0 && pila.Peek() != "INS" && pila.Peek() != "S" && pila.Peek() != "EOF")
                        {
                            pila.Pop();
                        }
                    }
                }
            }
        }

        // --- TRADUCTORES DE MENSAJES DE ERROR ---

        private string TraducirToken(string token)
        {
            switch (token)
            {
                case "PR1": return "INICIO (INI)";
                case "PR2": return "FUNCION (FUNC)";
                case "PR11": return "SI";
                case "PR12": return "SINO";
                case "PR13": return "ENCASO";
                case "PR15": return "MIENTRAS (MIENT)";
                case "PR16": return "REPETIR (REPT)";
                case "PR18": return "POR";
                case "CE1": return "paréntesis de apertura '('";
                case "CE2": return "paréntesis de cierre ')'";
                case "CE3": return "llave de apertura '{'";
                case "CE4": return "llave de cierre '}'";
                case "CE7": return "coma ','";
                case "CE8": return "punto y coma ';'";
                case "CE20": return "dos puntos ':'";
                case "IDV": return "un identificador de variable";
                case "IDF": return "un identificador de función";
                case "ASIG": return "operador de asignación '='";
                case "CNU": return "un valor numérico";
                case "CAD": return "una cadena de texto";
                default:
                    if (token.StartsWith("PR")) return $"la palabra reservada {token}";
                    if (token.StartsWith("OPA")) return $"un operador aritmético";
                    if (token.StartsWith("OPR")) return $"un operador relacional";
                    if (token.StartsWith("OPL")) return $"un operador lógico";
                    return $"'{token}'";
            }
        }

        private string ObtenerAyudaSintaxis(string noTerminal)
        {
            switch (noTerminal)
            {
                case "IN01": return "Sintaxis esperada: INI { <instrucciones> }";
                case "IN04": return "Sintaxis de SI: SI (<Condicion>) { <Instrucciones> } SINO { <Instrucciones> }";
                case "IN05": return "Sintaxis de ENCASO: ENCASO (<IDV>) { <Valor>: <Instrucciones> ROMPER; DFCT: <Instrucciones> ROMPER; }";
                case "IN06": return "Sintaxis de MIENTRAS: MIENT (<Condicion>) { <Instrucciones> }";
                case "IN07": return "Sintaxis de REPETIR: REPT { <Instrucciones> } HASTA (<Condicion>);";
                case "IN08": return "Sintaxis de POR: POR (<Asignacion> <Condicion>; <Paso>) { <Instrucciones> }";
                case "IN19": return "Sintaxis de FUNCION: FUNC <Tipo> <IDF> (<Parametros>) { <Instrucciones> REGR <Valor>; }";
                case "EXP":
                case "VALOR": return "Falta un operando (variable, número o cadena).";
                case "POT_P":
                case "TERM_P":
                case "EXP_P": return "Falta operador aritmético o terminar la línea con ';'.";
                case "COND": return "Se esperaba una condición lógica o relacional válida.";
                case "TIPO_VAR": return "Se requiere declarar el tipo de dato (ENT, DEC, TXT, BOOL, CAR).";
                case "ASIG_OPC": return "Falta el operador de asignación '=' o el cierre con ';'.";
                default: return "Verifique la escritura de la instrucción según el lenguaje GatoSabe.";
            }
        }

        private bool EsTerminalSintactico(string simbolo)
        {
            // Reconoce todos los terminales de ACT 3. Definiciones Regulares
            return simbolo.StartsWith("PR") || simbolo.StartsWith("CE") ||
                   simbolo.StartsWith("OPA") || simbolo.StartsWith("OPL") ||
                   simbolo.StartsWith("OPR") || simbolo == "IDV" || simbolo == "IDF" ||
                   simbolo == "CNU" || simbolo == "CAD" || simbolo == "CAR" ||
                   simbolo == "ASIG" || simbolo == "EOF";
        }

        // -------------------- Resto de métodos de tu clase original --------------------

        private void ReAnalizarTodo()
        {
            tokensGlobales.Clear();
            for (int i = 0; i < scintilla1.Lines.Count; i++) ReAnalizarLinea(i);
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
            foreach (var tk in tokensGlobales) PintarToken(tk.linea, tk.columna, tk.token.Length, tk.estilo);
        }

        private void Scintilla2_StyleNeeded(object sender, StyleNeededEventArgs e)
        {
            foreach (var tk in tokensTokens2) PintarTokenEnScintilla2(tk.linea, tk.columna, tk.token.Length, tk.estilo);
        }

        private void PintarToken(int linea, int columnaInicio, int longitud, int estilo)
        {
            try { int pos = scintilla1.Lines[linea].Position + columnaInicio; scintilla1.StartStyling(pos); scintilla1.SetStyling(longitud, estilo); } catch { }
        }

        private void PintarTokenEnScintilla2(int linea, int columnaInicio, int longitud, int estilo)
        {
            try { if (linea < 0 || linea >= scintilla2.Lines.Count) return; int pos = scintilla2.Lines[linea].Position + columnaInicio; scintilla2.StartStyling(pos); scintilla2.SetStyling(longitud, estilo); } catch { }
        }

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
            var sim = new Simbolo() { Numero = contadorSimbolos, Nombre = id, Tipo = tipo, Valor = valor };
            tablaSimbolos.Add(id, sim);
            contadorSimbolos++;
            return sim.Numero;
        }

        private void RegistrarFuncion(string nombre, string tipoRetorno, int lineaInicio)
        {
            string id = nombre.Trim();
            if (!tablaFunciones.ContainsKey(id))
            {
                var f = new Funcion() { Nombre = id, TipoRetorno = tipoRetorno, LineaInicio = lineaInicio, LineaCuerpoInicio = 0, LineaCuerpoFin = 0 };
                tablaFunciones.Add(id, f);
            }
        }

        private void AgregarParametroFuncion(string nombreFuncion, string tipoParam, string nombreParam)
        {
            if (!tablaFunciones.ContainsKey(nombreFuncion)) return;
            tablaFunciones[nombreFuncion].Parametros.Add((tipoParam, nombreParam));
        }

        private void LlenarDgvErrores()
        {
            dgvErrores.Rows.Clear();
            foreach (var err in listaErrores) dgvErrores.Rows.Add(err.linea, err.mensaje);
        }

        private void LlenarDgvErroresSintaxis()
        {
            if (dgvErroresSintaxis != null)
            {
                dgvErroresSintaxis.Rows.Clear();
                foreach (var err in erroresSintacticos) dgvErroresSintaxis.Rows.Add(err.Linea, err.Mensaje);
            }
        }

        private void LlenarTablaSimbolos()
        {
            dgvSimbolos.Rows.Clear();
            foreach (var sim in tablaSimbolos.Values.OrderBy(s => s.Numero)) dgvSimbolos.Rows.Add(sim.Numero, sim.Nombre, sim.Tipo, sim.Valor);
        }

        private void LlenarTablaFunciones()
        {
            dgvFunciones.Rows.Clear();
            foreach (var f in tablaFunciones.Values)
            {
                string listaParams = string.Join(", ", f.Parametros.Select(p => $"{p.tipo} {p.nombre}"));
                string cuerpo = (f.LineaCuerpoInicio > 0 && f.LineaCuerpoFin > 0) ? $"{f.LineaCuerpoInicio} - {f.LineaCuerpoFin}" : "—";
                dgvFunciones.Rows.Add(f.Nombre, f.TipoRetorno, listaParams, cuerpo);
            }
        }

        private string ObtenerValorAsignadoEnLinea(string lineaTexto, int columnaId)
        {
            int idxAsig = lineaTexto.IndexOf('=', columnaId);
            if (idxAsig == -1) return "";
            int idxPuntoComa = lineaTexto.IndexOf(';', idxAsig);
            if (idxPuntoComa == -1) idxPuntoComa = lineaTexto.Length;
            string valor = lineaTexto.Substring(idxAsig + 1, idxPuntoComa - idxAsig - 1);
            return valor.Trim();
        }

        private void GuardarArchivo()
        {
            if (HayTextoConError())
            {
                var result = MessageBox.Show("¿Estás seguro de guardar el código fuente si hay errores presentes?", "Advertencia", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.No) return;
            }
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

        private void btnEjecutar_Click(object sender, EventArgs e)
        {
            tokensGlobales.Clear();
            tokensTokens2.Clear();
            listaErrores.Clear();
            tablaSimbolos.Clear();
            tablaFunciones.Clear();
            dgvErroresSintaxis.Rows.Clear();
            contadorSimbolos = 1;
            scintilla2.Text = "";
            intTotalErrores = 0;

            tokensSintactico.Clear();
            erroresSintacticos.Clear();

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

                        if (IsTokenInvalido(resultado))
                        {
                            listaErrores.Add((lineaToken, resultado));
                            AgregarTokenATabla(resultado, 9);
                            continue;
                        }

                        // =========================================================
                        // --- INICIO DE NORMALIZACIÓN PARA TABLA SINTÁCTICA ---
                        string tokenSintactico = resultadoNormalizado;

                        // 1. Quitar textos extra (ej. "CN Entera" -> "CN")
                        if (tokenSintactico.Contains(" "))
                            tokenSintactico = tokenSintactico.Split(' ')[0];

                        // 2. Ajustar numéricos genéricos según tu GLC
                        if (tokenSintactico == "CN")
                            tokenSintactico = "CNU";

                        // 3. LIMPIEZA CRÍTICA: Quitar números de tabla de símbolos (ej. "IDV1" -> "IDV")
                        if (tokenSintactico.StartsWith("IDV"))
                            tokenSintactico = "IDV";
                        if (tokenSintactico.StartsWith("IDF"))
                            tokenSintactico = "IDF";

                        // 4. Agregar a la lista sintáctica (ignorando comentarios)
                        if (!tokenSintactico.StartsWith("COM"))
                        {
                            tokensSintactico.Add((tokenSintactico, lexema, lineaToken));
                        }
                        // --- FIN DE NORMALIZACIÓN ---
                        // =========================================================

                        if (lexema.Equals("FUNC", StringComparison.OrdinalIgnoreCase))
                        {
                            esDeclaracionFuncion = true;
                            AgregarTokenATabla(resultadoNormalizado, estiloToken);
                            continue;
                        }

                        if (resultadoNormalizado.StartsWith("PR"))
                        {
                            string posibleTipo = ObtenerTipoDesdePR(resultadoNormalizado);
                            if (!string.IsNullOrEmpty(posibleTipo)) tipoActual = posibleTipo;
                            AgregarTokenATabla(resultadoNormalizado, estiloToken);
                            continue;
                        }

                        if (resultadoNormalizado.StartsWith("IDF"))
                        {
                            nombreFuncionActual = lexema;
                            RegistrarFuncion(nombreFuncionActual, tipoActual, lineaToken);
                            AgregarTokenATabla(resultadoNormalizado, estiloToken);
                            continue;
                        }

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

                        if (resultadoNormalizado.StartsWith("IDV") && !string.IsNullOrEmpty(tipoActual) && !dentroDeFuncion)
                        {
                            string valor = ObtenerValorAsignadoEnLinea(linea, columna);
                            int num = RegistrarIdentificador(lexema, tipoActual, valor);
                            AgregarTokenATabla(resultadoNormalizado + num.ToString(), estiloToken);
                            continue;
                        }

                        if (resultadoNormalizado.StartsWith("IDV"))
                        {
                            AgregarTokenATabla(resultadoNormalizado, estiloToken);
                            continue;
                        }

                        AgregarTokenATabla(resultadoNormalizado, estiloToken);
                    }
                    catch
                    {
                        listaErrores.Add((lineaToken, "ERROR_INTERNO"));
                        AgregarTokenATabla("ERROR_INTERNO", 9);
                    }
                }

                if (linea.Trim() == "{")
                {
                    dentroDeFuncion = true;
                    if (!string.IsNullOrEmpty(nombreFuncionActual)) tablaFunciones[nombreFuncionActual].LineaCuerpoInicio = i + 1;
                }

                if (linea.Trim() == "}")
                {
                    dentroDeFuncion = false;
                    if (!string.IsNullOrEmpty(nombreFuncionActual)) tablaFunciones[nombreFuncionActual].LineaCuerpoFin = i + 1;
                }

                if (esDeclaracionFuncion && linea.Contains(")"))
                {
                    esDeclaracionFuncion = false;
                    tipoActual = "";
                }

                NuevaLineaTokens();
            }

            if (listaErrores.Count == 0 && tokensSintactico.Count > 0)
            {
                EjecutarAnalisisSintactico();
            }

            intTotalErrores = listaErrores.Count;
            lblTotalErrores.Text = "Total de Errores Léxicos: " + intTotalErrores.ToString();
            LlenarDgvErrores();
            LlenarDgvErroresSintaxis();
            LlenarTablaSimbolos();
            LlenarTablaFunciones();
            ResaltarLineasError();

            scintilla2.Refresh();
        }

        private void GuardarTokens()
        {
            if (string.IsNullOrWhiteSpace(scintilla2.Text))
            {
                MessageBox.Show("No es posible guardar Tokens porque no existen.", "Operación Cancelada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (listaErrores.Count > 0)
            {
                MessageBox.Show("No es posible guardar Tokens. Existen Errores de Tokens.", "Operación Cancelada", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
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

        private void btnGuardarPrograma_Click(object sender, EventArgs e) => GuardarArchivo();
        private void btnCargarPrograma_Click(object sender, EventArgs e) => CargarArchivo();
        private void btnGuardarArchivoTokens_Click(object sender, EventArgs e) => GuardarTokens();

        private void NuevaLineaTokens() { scintilla2.AppendText("\n"); }

        private void AgregarTokenATabla(string token, int estilo = -1)
        {
            string t = token.Replace("TOKEN: ", "");

            if (scintilla2.Lines.Count == 1 && scintilla2.Lines[0].Text == "")
            {
                scintilla2.AppendText(t);
                if (estilo >= 0) tokensTokens2.Add((0, 0, t, estilo));
                return;
            }

            int ultima = scintilla2.Lines.Count - 1;
            string textoUltima = scintilla2.Lines[ultima].Text;
            int columnaInicio = (textoUltima.Length == 0) ? 0 : textoUltima.Length + 1;

            if (textoUltima.Trim().Length == 0) scintilla2.AppendText(t);
            else scintilla2.AppendText(" " + t);

            if (estilo >= 0) tokensTokens2.Add((ultima, columnaInicio, t, estilo));
        }

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

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { }
        private void dgvFunciones_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void scintilla2_CharAdded(object sender, CharAddedEventArgs e) { }

        private void scintilla1_CharAdded(object sender, CharAddedEventArgs e)
        {
            int pos = scintilla1.CurrentPosition;
            if (pos >= 5)
            {
                int maxCheck = Math.Min(pos, 20);
                string texto = scintilla1.GetTextRange(pos - maxCheck, maxCheck);
                int count = 0;
                for (int i = texto.Length - 1; i >= 0; i--) { if (texto[i] == ' ') count++; else break; }
                if (count > 0 && count % 5 == 0)
                {
                    int tabs = count / 5;
                    scintilla1.DeleteRange(pos - count, count);
                    for (int i = 0; i < tabs; i++) scintilla1.InsertText(pos - count + i, "\t");
                    scintilla1.GotoPosition(pos - count + tabs);
                }
            }
        }

        private bool HayTextoConError()
        {
            for (int i = 0; i < scintilla1.TextLength; i++)
            {
                int style = scintilla1.GetStyleAt(i);
                if (style == 9) return true;
            }
            return false;
        }
    }
}