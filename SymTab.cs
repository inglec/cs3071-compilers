using System;

namespace Tastier {
    public class Obj { // properties of declared symbol
        public string name; // its name
        public int kind;    // 0 = var, 1 = proc, 2 = scope, 3 = constant.
        public int type;    // its type if var (undef for proc); 0 = undef, 1 = int, 2 = bool

        // Constant attributes.
        public int value;   // value if kind == constant

        // Array attributes.
        public int dims;    // dimensions of variable; 0 = scalar, 1 = 1D array, 2 = 2D array.
        public int size1;   // size of 1st dim.
        public int size2;   // size of 2nd dim.
        public int start1;  // start index of 1st dim.
        public int start2;  // start index of 2nd dim.

        public int level;   // lexic level: 0 = global; >= 1 local
        public int adr;     // address (displacement) in scope

        public Obj next;    // ptr to next object in scope
        // for scopes
        public Obj outer;   // ptr to enclosing scope
        public Obj locals;  // ptr to locally declared objects

        public int nextAdr; // next free address in scope
    }

    public class SymbolTable {
        // object kinds
        const int var = 0, proc = 1, scope = 2, constant = 3;
        String[] kinds = {"var", "proc", "scope", "const"};  //String representation of values.

        // variable / constant types
        const int undef = 0, integer = 1, boolean = 2;
        String[] types = {"undef", "int", "bool"};  //String representation of values.

        public Obj topScope; // topmost procedure scope
        public int curLevel; // nesting level of current scope
        public Obj undefObj; // object node for erroneous symbols

        public bool mainPresent;

        Parser parser;

        /**
         * Symbol Table Constructor
         */
        public SymbolTable(Parser parser) {
            curLevel = -1;
            topScope = null;

            undefObj = new Obj();
            undefObj.name = "undef";
            undefObj.kind = var;
            undefObj.type = undef;
            undefObj.level = 0;
            undefObj.adr = 0;
            undefObj.next = null;

            this.parser = parser;
            mainPresent = false;
        }

        // open new scope and make it the current scope (topScope)
        public void OpenScope() {
            Obj scop = new Obj();
            scop.name = "";
            scop.kind = scope;
            scop.outer = topScope;
            scop.locals = null;
            scop.nextAdr = 0;   //Next address in stack frame.

            topScope = scop;    //Make this the current scope.
            curLevel++;
        }

        // close current scope
        public void CloseScope() {
            //Print scope locals to console with their kind, type and name.
            Console.WriteLine(";End of Scope (lvl " + curLevel + ")."); //Print level of scope.
            Obj current = topScope.locals;
            while (current != null) {
                Console.WriteLine(";  " + kinds[current.kind] + " " + types[current.type] + " " + current.name);
                current = current.next;
            }

            topScope = topScope.outer;  //Make the outer scope the current scope.
            curLevel--;
        }

        // open new sub-scope and make it the current scope (topScope)
        public void OpenSubScope() {
            // lexic level remains unchanged
            Obj scop = new Obj();
            scop.name = "";
            scop.kind = scope;
            scop.outer = topScope;
            scop.locals = null;
            // next available address in stack frame remains unchanged
            scop.nextAdr = topScope.nextAdr;

            topScope = scop;
        }

        // close current sub-scope
        public void CloseSubScope() {
            // update next available address in enclosing scope
            topScope.outer.nextAdr = topScope.nextAdr;
            // lexic level remains unchanged
            topScope = topScope.outer;
        }

        // create new object node in current scope
        public Obj NewObj(string name, int kind, int type) {
            Obj obj = new Obj();
            obj.name = name;
            obj.kind = kind;
            obj.type = type;
            obj.dims = 0;
            obj.level = curLevel;
            obj.next = null;

            Obj p = topScope.locals;
            Obj last = null;
            while (p != null) {
                if (p.name == name) {
                    parser.SemErr("name declared twice");
                }
                last = p;
                p = p.next;
            }

            //Add new object to end of linked list locals
            if (last == null) {
                topScope.locals = obj;
            }
            else {
                last.next = obj;
            }

            //set variable address to next free stack address
            if (kind == var) {
                obj.adr = topScope.nextAdr++;
            }
            return obj;
        }

        public void CreateArray(Obj obj, int dims, int size1, int size2, int start1, int start2) {
            obj.dims = dims;
            obj.size1 = size1;
            obj.size2 = size2;
            obj.start1 = start1;
            obj.start2 = start2;

            if (dims == 1) {
                topScope.nextAdr += size1 - 1;    // Allocate memory for 1D array.
            }
            else if (dims == 2) {
                topScope.nextAdr += (size1 * size2) - 1;    // Allocate memory for 2D array.
            }
        }

        // search for name in open scopes and return its object node
        public Obj Find(string name) {
            Obj obj;
            Obj scope = topScope;
            while (scope != null) { // for all open scopes
                obj = scope.locals;
                while (obj != null) { // for all objects in this scope
                    if (obj.name == name) {
                        return obj;
                    }
                    obj = obj.next;
                }
                scope = scope.outer;
            }
            parser.SemErr(name + " is undeclared");
            return undefObj;
        }

    } // end SymbolTable
} // end namespace
