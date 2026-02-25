using System;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// A compilation pass is an independent and indivisible step to be performed at graph compilation.
    /// It's registered when instantiating a compiler.
    /// Compilation pass should have no side effect and are just mutating passed graph and compilation data.
    /// Compilation is typically made up of several compilation passes.
    /// </summary>
    /*public*/ interface CompilationPass
    {
        /// <summary>
        /// Executes the compilation pass.
        /// This is called internally by the graph compiler.
        /// </summary>
        /// <param name="context">The compilation context.</param>
        /// <returns>true if the pass execution succeeded, false otherwise.</returns>
        public bool Execute(ref CompilationContext context);
    }

    /// <summary>
    /// A special compilation pass responsible for generating the compiled data.
    /// This is typically the last pass executed during the graph compilation pipe.
    /// </summary>
    /// <typeparam name="T">The type of the generated data.</typeparam>
    /*public*/ interface DataGenerationPass<T>
    {
        /// <summary>
        /// Executes the data generation pass.
        /// This is called internally by the graph compiler.
        /// </summary>
        /// <param name="context">The compilation context.</param>
        /// <returns>the generated data of type T, or null if the pass failed</returns>
        public T Execute(ref CompilationContext context);
    }

    /// <summary>
    /// A struct holding data passed through compilation passes during a graph compilation.
    /// </summary>
    /*public*/ struct CompilationContext
    {
        /// <summary>
        /// The mutable graph to be transformed.
        /// </summary>
        public IMutableGraph graph;
        /// <summary>
        /// The compilation data carried aside the graph.
        /// </summary>
        public CompilationData data;
        /// <summary>
        /// The compilation report.
        /// </summary>
        public CompilationReport report;
    }

    /// <summary>
    /// A struct holding outputs to a graph compilation.
    /// </summary>
    /// <typeparam name="T">The type of the generated data.</typeparam>
    /*public*/ struct CompilationResult<T>
    {
        /// <summary>
        /// The generated data.
        /// This is the actual result of the graph compilation.
        /// </summary>
        public T result;
        /// <summary>
        /// The input graph required to be compiled.
        /// </summary>
        public IReadOnlyGraph source;
        /// <summary>
        /// The final graph after transformations during the compilation.
        /// </summary>
        public IReadOnlyGraph finalGraph;
        /// <summary>
        /// The compilation report.
        /// </summary>
        public CompilationReport report;
    }

    /// <summary>
    /// The compiler is responsible to transform a graph into compiled data for runtime.
    /// It is designed in a modular way and comprised of a series of independent compilation passes.
    /// The last pass is a special one responsible for generating the generic compiled data.
    /// </summary>
    /// <typeparam name="T">The type of the generated data.</typeparam>
    /*public*/ class Compiler<T>
    {
        CompilationPass[] m_Passes;
        DataGenerationPass<T> m_FinalPass;

        /// <summary>
        /// The constructor of Compiler.
        /// </summary>
        /// <param name="dataGenerationPass">The data generation pass (the final pass).</param>
        /// <param name="passList">The list of compilation passes.</param>
        public Compiler(DataGenerationPass<T> dataGenerationPass, params CompilationPass[] passList)
        {
            if (dataGenerationPass == null || passList == null)
                throw new ArgumentNullException();

            m_Passes = passList;
            m_FinalPass = dataGenerationPass;
        }

        /// <summary>
        /// Compiles a given graph.
        /// Note that the passed graph is readonly and unaltered by the compilation.
        /// </summary>
        /// <param name="graph">The input graph to compile.</param>
        /// <returns>The result if the compilation.</returns>
        public CompilationResult<T> Compile(IReadOnlyGraph graph)
        {
            if (graph == null)
                throw new ArgumentNullException("Trying to compile a null graph");

            var context = new CompilationContext
            {
                graph = graph.Copy(),
                data = new CompilationData(),
                report = new CompilationReport(),
            };

            bool error = false;
            foreach (var pass in m_Passes)
            {
                try
                {
                    if (!pass.Execute(ref context))
                    {
                        error = true;
                        break;
                    }
                }
                catch (Exception e)
                {
                    Debug.Log($"Exception in Compilation Pass {pass} : {e.Message}. Compilation aborted {e.StackTrace}");
                    error = true;
                    break;
                }
            }

            T compiledGraph = default(T);
            IReadOnlyGraph finalGraph = null;
            if (!error)
            {
                compiledGraph = m_FinalPass.Execute(ref context);
                finalGraph = context.graph;
            }

            return new CompilationResult<T>
            {
                result = compiledGraph,
                source = graph,
                finalGraph = finalGraph,
                report = context.report,
            };
        }
    }
}
