module Tree

open Print
open LLVM.Generated.Core
open Microsoft.FSharp.Text.Lexing
open System.Collections.Generic
open System.Collections.ObjectModel
open System

type Op = 
    | Add | Sub | Mul | Div
    | BoolAnd | BoolOr | Not
    | Lt | Gt | LtEq | GtEq | Eq

type PType = Undef | Unit | Int of int | Bool | Func of PType * PType with
    override x.ToString() = fmt x

let parsePType str = match str with
    | "Unit" -> Unit
    | "Int" -> Int 32
    | "Bool" -> Bool

type Pos(startPos:Position, endPos:Position) =
    member x.StartPos = startPos
    member x.EndPos = endPos
    override x.ToString() = sprintf "s(%i,%i)e(%i,%i)" x.StartPos.Line x.StartPos.Column x.EndPos.Line x.EndPos.Column

type RefType = Local | Parameter

type Ref(name:string, ptype:PType, reftype: RefType) =
    member x.Name = name
    member x.PType = ptype
    member x.RefType = reftype
    member val ValueRef = new ValueRef(nativeint 0xDEAD0000) with get, set
    member x.IsUninitialised = x.ValueRef.Ptr.ToInt32() = 0xDEAD0000
    override x.ToString() = sprintf "%s:%s%s" x.Name (fmt x.PType) (if x.IsUninitialised then ":Uninit" else "")

type SRP = KeyValuePair<string, Ref>

type Annot<'a>(item:'a, pos:Pos) =
    let mutable refs:Map<string,Ref> = Map.empty;
    let filterRefs refType =
        Map.filter (fun k (v:Ref) -> v.RefType = refType) refs
        |> Map.toSeq |> Seq.map (fun (k,v) -> v)

    member x.Refs = refs
    member x.AddRef(ref:Ref) = refs <- refs.Add(ref.Name, ref)
    member x.AddRefs(refs) = Map.iter (fun name ref -> x.AddRef(ref)) refs
    member x.GetRef(name) = Map.find name refs
    member x.LocalRefs with get () = filterRefs Local
    member x.ParamRefs with get () = filterRefs Parameter

    member val LocalVars:list<Ref> = [] with get, set

    member x.Pos = pos
    member x.Item = item
    member val PType = Undef with get, set

    override x.ToString() = fmt x.Item + match x.PType with
        | Undef -> ""
        | _ -> ":" + fmt x.PType
        (*":" + x.Pos.ToString() +
        ":" + fmt (List.ofSeq <| Seq.map (fun (kvp:KeyValuePair<string,Ref>) -> kvp.Value) x.Refs)*)
        

type Expr =
    | ConstInt of int
    | ConstBool of bool
    | ConstUnit
    | Var of string
    | Binop of Op * ExprA * ExprA
    | Call of string * list<ExprA>
    | Assign of string * ExprA
    | DeclVar of string * (*Assign*) ExprA
    | Print of ExprA
    | Return of ExprA
    | If of ExprA * ExprA * ExprA
    | While of ExprA * ExprA
    | Seq of ExprA * ExprA

and ExprA = Annot<Expr>

type Param(name: string, ptype: PType) =
    member x.Name = name
    member x.PType = ptype
    override x.ToString() = sprintf "%s:%s" name (fmt x.PType)

type Decl = 
    | Proc of (*name*) string * (*params*) list<Param> * (*returnType*) PType * ExprA

type DeclA = Annot<Decl>


type Program = list<DeclA>

type Func(name: string, func: ValueRef, params: Map<string, ValueRef>) =
    member x.Name = name
    member x.Func = func
    member x.Params = params

type Environ(module_: ModuleRef, enclosingFunc: Ref) =
    member x.Module = module_
    member x.EnclosingFunc = enclosingFunc


let rec foldAST branchFunc leafFunc (exprA:ExprA) =
    let fAST e = foldAST branchFunc leafFunc e
    let bf1 branch e = branchFunc branch <| [fAST e]
    let bf branch es = branchFunc branch <| List.map fAST es
    match exprA.Item with
        | ConstInt _ -> leafFunc exprA
        | ConstBool _ -> leafFunc exprA
        | ConstUnit -> leafFunc exprA
        | Var n -> leafFunc exprA
        | Binop (op, l, r) -> bf exprA [l; r]
        | Call (name, exprAs) -> bf exprA exprAs
        | Assign (name, innerExprA) -> bf1 exprA innerExprA
        | DeclVar (name, assignA) -> bf1 exprA assignA
        | Print e -> bf1 exprA e
        | Return e -> bf1 exprA e
        | If (test, then_, else_) -> bf exprA [test; then_; else_]
        | While (test, body) -> bf exprA [test; body]
        | Seq (e1A, e2A) -> bf exprA [e1A; e2A]