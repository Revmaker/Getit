using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Carlabs.Getit
{
    /// <summary>
    /// Builds a GraphQL query from the Query Object. For parameters it
    /// support simple parameters, ENUMs, Lists, and Objects.
    /// For selections fields it supports sub-selects with params as above.
    ///
    /// Most all structures can be recursive, and are unwound as needed
    ///
    /// </summary>
    public class QueryStringBuilder : IQueryStringBuilder
    {
        public StringBuilder QueryString { get; }
        private const int IndentSize = 4;

        public QueryStringBuilder()
        {
            QueryString = new StringBuilder();
        }

        /// <summary>
        /// Clear the QueryStringBuilder and all that entails
        /// </summary>
        public void Clear()
        {
            QueryString.Clear();
        }

        /// <summary>
        /// Recurse an object which could be a primitive or more
        /// complex structure. This will return a string of the value
        /// at the current level. Recursion terminates when at a terminal
        /// (primitive).
        /// </summary>
        /// <param name="value"></param>
        /// <returns>string</returns>
        /// <exception cref="InvalidDataException">Invalid Object Type in Param List</exception>
        public string BuildQueryParam(object value)
        {
            // Nicely use the pattern match

            switch (value)
            {
                // String to EnumHelper are all treated as a
                // primitive value
                case string strValue:
                    return "\"" + strValue + "\"";

                case int intValue:
                    return intValue.ToString();

                case long longValue:
                    return longValue.ToString();

                case float floatValue:
                    return floatValue.ToString(CultureInfo.CurrentCulture);

                case double doubleValue:
                    return doubleValue.ToString(CultureInfo.CurrentCulture);

                case EnumHelper enumValue:
                    return enumValue.ToString();

                // All below are non-primitives that will recurse
                // until the structure resolves into primitives

                case KeyValuePair<string, object> kvValue:
                    StringBuilder keyValueStr = new StringBuilder();

                    keyValueStr.Append($"{kvValue.Key}:{BuildQueryParam(kvValue.Value)}");
                    return keyValueStr.ToString();

                case IList listValue:
                    StringBuilder listStr = new StringBuilder();

                    listStr.Append("[");
                    bool hasList = false;
                    foreach (var obj in listValue)
                    {
                        listStr.Append(BuildQueryParam(obj) + ", ");
                        hasList = true;
                    }

                    // strip comma-space from local list if not empty

                    if (hasList)
                    {
                        listStr.Length -= 2;
                    }

                    listStr.Append("]");

                    return listStr.ToString();

                case IDictionary dictValue:
                    StringBuilder dictStr = new StringBuilder();

                    dictStr.Append("{");
                    bool hasType = false;
                    foreach (var dictObj in (Dictionary<string, object>) dictValue)
                    {
                        dictStr.Append(BuildQueryParam(dictObj) + ", ");
                        hasType = true;
                    }

                    // strip comma-space from type if not empty.
                    // Not sure if this should generate code
                    // or Toss, depends on if in GQL `name:{}` is valid in
                    // any circumstance

                    if (hasType)
                    {
                        dictStr.Length -= 2;
                    }

                    dictStr.Append("}");

                    return dictStr.ToString();

                default:
                    throw new InvalidDataException("Unsupported Query Parameter, Type Found : " + value.GetType());
            }
        }

        /// <summary>
        /// This take all parameter data
        /// and builds the string. This will look in the query and
        /// use the WhereMap for the list of data. The data can be
        /// most any type as long as it's one that we support. Will
        /// resolve nested structures
        /// </summary>
        /// <param name="query">The Query</param>
        public void AddParams(IQuery query)
        {
            // safe-tee check

            if (query == null)
            {
                return;
            }

            // Build the param list from the name value pairs.
            // All entries have a `name`:`value` looking format. The
            // BuildQueryParam's will recurse any nested data elements

            bool hasParams = false;
            foreach (var param in query.WhereMap)
            {
                QueryString.Append($"{param.Key}:");
                QueryString.Append(BuildQueryParam(param.Value) + ", ");
                hasParams = true;
            }

            // Remove the last comma and space that always trails if we have params!

            if (hasParams)
            {
                QueryString.Length--;
                QueryString.Length--;
            }
        }

        /// <summary>
        /// Adds fields to the query sting. This will use the SelectList
        /// structure from the query to build the graphql select list. This
        /// will recurse as needed to pick up sub-selects that can contain
        /// parameter lists.
        /// </summary>
        /// <param name="query">The Query</param>
        /// <param name="indent">Indent characters, default 0</param>
        /// <exception cref="ArgumentException">Invalid Object in Field List</exception>
        public void AddFields(IQuery query, int indent = 0)
        {
            // Build the param list from the name value pairs. NOTE
            // This will build array or objects differently based on the
            // type of the value object

            string strPad = new String(' ', indent);

            foreach (var field in query.SelectList)
            {
                switch (field)
                {
                    case string _:
                        QueryString.Append(strPad + $"{field}\n");
                        break;
                    case Query query1:
                        QueryStringBuilder subQuery = new QueryStringBuilder();
                        QueryString.Append($"{subQuery.Build(query1, indent)}\n");
                        break;
                    default:
                        throw new ArgumentException("Invalid Field Type Specified, must be `string` or `Query`");
                }
            }
        }

        /// <summary>
        /// Adds a comment to the Select list part of the Query. Comments
        /// may be separated by a newline and those will expand to individual
        /// comment line. Formatting for graphQL '#' comments will happen here
        /// </summary>
        /// <param name="comments">Simple Comment</param>
        /// <param name="indent">Indent characters, default 0</param>
        public void AddComments(string comments, int indent = 0)
        {
            if (String.IsNullOrEmpty(comments))
                return;

            string pad = new String(' ', indent);
            string comment = comments.Replace("\n", $"\n{pad}# ");

            QueryString.Append(pad + "# " + comment + "\n");
        }

        /// <summary>
        /// Build the entire query into a string. This will take
        /// the query object and build a graphql query from it. This
        /// returns the query, but not the outer block. This is done so
        /// you can use the output to batch the queries
        /// </summary>
        /// <param name="query">The Query</param>
        /// <param name="indent">Indent characters, default = 0</param>
        /// <returns>GraphQL query string without outer block</returns>
        public string Build(IQuery query, int indent = 0)
        {
            string pad = new String(' ', indent);
            string prevPad = pad;

            // if we have an alias add it before the name, no padding for next field which is the name!

            if (!String.IsNullOrWhiteSpace(query.AliasName))
            {
                QueryString.Append(pad + $"{query.AliasName}:");
                pad = "";
            }

            // here we go, start with the name

            QueryString.Append(pad + query.QueryName);

            // If we have params must add in parens, if
            // no params in the query then must skip the parens.

            if (query.WhereMap.Count > 0)
            {
                QueryString.Append("(");
                AddParams(query);
                QueryString.Append(")");
            }

            // now build the Field list, bump padding as we are inside a query (field block)

            indent += IndentSize;

            QueryString.Append("{\n");

            // Stuff any comments in the field (select) section of the query

            AddComments(query.QueryComment, indent);
            AddFields(query, indent);

            QueryString.Append(prevPad + "}");

            return QueryString.ToString();
        }
    }
}
