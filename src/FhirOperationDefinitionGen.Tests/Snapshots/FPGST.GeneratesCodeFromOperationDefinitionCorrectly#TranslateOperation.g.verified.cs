//HintName: TranslateOperation.g.cs
#nullable enable

/// <summary>
/// Concept Translation
/// Translate a code from one value set to another, based on the existing value set and concept maps resources, and/or other additional knowledge available to the server.  One (and only one) of the in parameters (code, coding, codeableConcept) must be provided, to identify the code that is to be translated.   The operation returns a set of parameters including a 'result' for whether there is an acceptable match, and a list of possible matches. Note that the list of matches may include notes of codes for which mapping is specifically excluded, so implementers have to check the match.equivalence for each match
/// </summary>
public class TranslateOperation
{
    /// <summary>
    /// Name used to invoke the operation.
    /// </summary>
    public const string Name = "translate";

    /// <summary>
    /// The operation's request parameters.
    /// </summary>
    public class Request
    {
        /// <summary>
        /// A canonical URL for a concept map. The server must know the concept map (e.g. it is defined explicitly in the server's concept maps, or it is defined implicitly by some code system known to the server.
        /// </summary>
        public Hl7.Fhir.Model.Uri? Url { get; set; }
        /// <summary>
        /// The concept map is provided directly as part of the request. Servers may choose not to accept concept maps in this fashion.
        /// </summary>
        public Hl7.Fhir.Model.ConceptMap? ConceptMap { get; set; }
        /// <summary>
        /// The identifier that is used to identify a specific version of the concept map to be used for the translation. This is an arbitrary value managed by the concept map author and is not expected to be globally unique. For example, it might be a timestamp (e.g. yyyymmdd) if a managed version is not available.
        /// </summary>
        public Hl7.Fhir.Model.String? ConceptMapVersion { get; set; }
        /// <summary>
        /// The code that is to be translated. If a code is provided, a system must be provided
        /// </summary>
        public Hl7.Fhir.Model.Code? Code { get; set; }
        /// <summary>
        /// The system for the code that is to be translated
        /// </summary>
        public Hl7.Fhir.Model.Uri? System { get; set; }
        /// <summary>
        /// The version of the system, if one was provided in the source data
        /// </summary>
        public Hl7.Fhir.Model.String? Version { get; set; }
        /// <summary>
        /// Identifies the value set used when the concept (system/code pair) was chosen. May be a logical id, or an absolute or relative location. The source value set is an optional parameter because in some cases, the client cannot know what the source value set is. However, without a source value set, the server may be unable to safely identify an applicable concept map, and would return an error. For this reason, a source value set SHOULD always be provided. Note that servers may be able to identify an appropriate concept map without a source value set if there is a full mapping for the entire code system in the concept map, or by manual intervention
        /// </summary>
        public Hl7.Fhir.Model.Uri? Source { get; set; }
        /// <summary>
        /// A coding to translate
        /// </summary>
        public Hl7.Fhir.Model.Coding? Coding { get; set; }
        /// <summary>
        /// A full codeableConcept to validate. The server can translate any of the coding values (e.g. existing translations) as it chooses
        /// </summary>
        public Hl7.Fhir.Model.CodeableConcept? CodeableConcept { get; set; }
        /// <summary>
        /// Identifies the value set in which a translation is sought. May be a logical id, or an absolute or relative location. If there's no target specified, the server should return all known translations, along with their source
        /// </summary>
        public Hl7.Fhir.Model.Uri? Target { get; set; }
        /// <summary>
        /// identifies a target code system in which a mapping is sought. This parameter is an alternative to the target parameter - only one is required. Searching for any translation to a target code system irrespective of the context (e.g. target valueset) may lead to unsafe results, and it is at the discretion of the server to decide when to support this operation
        /// </summary>
        public Hl7.Fhir.Model.Uri? Targetsystem { get; set; }
        /// <summary>
        /// if this is true, then the operation should return all the codes that might be mapped to this code. This parameter reverses the meaning of the source and target parameters
        /// </summary>
        public Hl7.Fhir.Model.Boolean? Reverse { get; set; }

        /// <summary>
        /// Convert this object to its FHIR Parameters representation.
        /// </summary>
        /// <returns>A FHIR Parameters instance.</returns>
        public Hl7.Fhir.Model.Parameters ToParameters()
        {
            var parameters = new Parameters();

            parameters.Add("url", this.Url);
            parameters.Add("conceptMap", this.ConceptMap);
            parameters.Add("conceptMapVersion", this.ConceptMapVersion);
            parameters.Add("code", this.Code);
            parameters.Add("system", this.System);
            parameters.Add("version", this.Version);
            parameters.Add("source", this.Source);
            parameters.Add("coding", this.Coding);
            parameters.Add("codeableConcept", this.CodeableConcept);
            parameters.Add("target", this.Target);
            parameters.Add("targetsystem", this.Targetsystem);
            parameters.Add("reverse", this.Reverse);

            return parameters;
        }
    }

    /// <summary>
    /// The operation's response parameters.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// True if the concept could be translated successfully. The value can only be true if at least one returned match has an equivalence which is not  unmatched or disjoint
        /// </summary>
        public Hl7.Fhir.Model.Boolean Result { get; set; }
        /// <summary>
        /// Error details, for display to a human. If this is provided when result = true, the message carries hints and warnings (e.g. a note that the matches could be improved by providing additional detail)
        /// </summary>
        public Hl7.Fhir.Model.String Message { get; set; }

        /// <summary>
        /// Convert this object to its FHIR Parameters representation.
        /// </summary>
        /// <returns>A FHIR Parameters instance.</returns>
        public Hl7.Fhir.Model.Parameters ToParameters()
        {
            var parameters = new Parameters();

            parameters.Add("result", this.Result);
            parameters.Add("message", this.Message);

            return parameters;
        }
    }
}
