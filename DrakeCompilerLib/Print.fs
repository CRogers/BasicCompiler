﻿module Print

open Microsoft.FSharp.Reflection
open System

let rec fmt_list(x):string =
    let t = x.GetType()
    let union, fields = FSharpValue.GetUnionFields(x, t)
    match union.Name with
        | "Empty" -> ""
        | "Cons"  -> 
            let current = fmt fields.[0]
            let next = fmt_list fields.[1]
            if next.Equals("") then current
            else sprintf "%s; %s" current next

and fmt(x):string =
    let t = x.GetType()
    //if t.Name.StartsWith("FSharpList") then
    //    x.ToString()
    if FSharpType.IsUnion(t) then
        let union, fields = FSharpValue.GetUnionFields(x, t)
        if union.Name = "Empty" then "[]"
        elif union.Name = "Cons" then
            sprintf "[%s]" (fmt_list x)
        elif fields.Length = 0 then
            union.Name
        else
            let fieldsStr = String.Join(" ", Array.map fmt fields)
            sprintf "(%s %s)" union.Name fieldsStr
    elif t.Name.StartsWith("FSharpRef") then
        let v = t.GetProperty("contents").GetValue(x)
        sprintf "ref{%s}" <| v.ToString()
    else  
        x.ToString()

let listTS<'a> (xs:seq<'a>) = sprintf "[%s]" <| System.String.Join("; ", Seq.map (fun x -> x.ToString()) xs)
let refTS<'a> (x:ref<'a>) = (!x).ToString()

let printfmt x = Console.WriteLine(fmt x)