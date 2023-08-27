//HintName: ClosureOperation.g.cs
#nullable enable

/// <summary>
/// Closure Table Maintenance
/// This operation provides support for ongoing maintenance of a client-side [transitive closure table](https://en.wikipedia.org/wiki/Transitive_closure#In_graph_theory) based on server-side terminological logic. For details of how this is used, see [Maintaining a Closure Table](terminology-service.html#closure)
/// </summary>
public class ClosureOperation
{
    /// <summary>
    /// Name used to invoke the operation.
    /// </summary>
    public const string Name = "closure";

    /// <summary>
    /// The operation's request parameters.
    /// </summary>
    public class Request
    {
        /// <summary>
        /// The name that defines the particular context for the subsumption based closure table
        /// </summary>
        public Hl7.Fhir.Model.String Name { get; set; }
        /// <summary>
        /// Concepts to add to the closure table
        /// </summary>
        public Hl7.Fhir.Model.Coding? Concept { get; set; }
        /// <summary>
        /// A request to resynchronise - request to send all new entries since the nominated version was sent by the server
        /// </summary>
        public Hl7.Fhir.Model.String? Version { get; set; }

        /// <summary>
        /// Convert this object to its FHIR Parameters representation.
        /// </summary>
        /// <returns>A FHIR Parameters instance.</returns>
        public Hl7.Fhir.Model.Parameters ToParameters()
        {
            var parameters = new Parameters();

            parameters.Add("name", this.Name);
            parameters.Add("concept", this.Concept);
            parameters.Add("version", this.Version);

            return parameters;
        }
    }

    /// <summary>
    /// The operation's response parameters.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// A list of new entries (code / system --> code/system) that the client should add to its closure table. The only kind of entry mapping equivalences that can be returned are equal, specializes, subsumes and unmatched
        /// </summary>
        public Hl7.Fhir.Model.ConceptMap Return { get; set; }

        /// <summary>
        /// Convert this object to its FHIR Parameters representation.
        /// </summary>
        /// <returns>A FHIR Parameters instance.</returns>
        public Hl7.Fhir.Model.Parameters ToParameters()
        {
            var parameters = new Parameters();

            parameters.Add("return", this.Return);

            return parameters;
        }
    }
}
