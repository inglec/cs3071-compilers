using System;

namespace Tastier {
    public class Obj { // properties of declared symbol
        public string name; // its name
        public int kind;    // 0 = var, 1 = proc, 2 = scope, 3 = constant.
        public int type;    // its type if var (undef for proc); 0 = undef, 1 = int, 2 = bool

        // Constant attributes.
        public int value;   // value if kind == constant

        // Array attributes.
        public int dims = 0;    // dimensions of variable; 0 = scalar, 1 = 1D array, 2 = 2D array.
        public int size1 = 0;   // size of 1st dim.
        public int size2 = 0;   // size of 2nd dim.

        // Function attributes
        public int args = 0; // Number of function parameters.

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
        const int undef = 0, integer = 1, boolean = 2, new_data_type = 3, new_data_type_instance = 4;
        String[] types = {"undef", "int", "bool", "new data type", "new data type instance"};  //String representation of values.

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
            Console.WriteLine("; End of Scope (lvl " + curLevel + ")."); //Print level of scope.
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
            return NewObj(name, kind, type, false); // Create a var within current scope
        }

        public Obj NewObj(string name, int kind, int type, bool memberVar) {
            Obj obj = new Obj();
            obj.name = name;
            obj.kind = kind;
            obj.type = type;
            obj.level = curLevel;
            obj.next = null;

            Obj curr = topScope.locals;
            Obj last = null;
            while (curr != null) {
                if (curr.name == name)
                    parser.SemErr(name + " declared twice");
                last = curr;
                curr = curr.next;
            }

            // Don't add member variables to scope's locals
            if (!memberVar) {
                //Add new object to end of linked list locals
                if (last == null)
                    topScope.locals = obj;
                else
                    last.next = obj;
            }

            // Set variable address to next free stack address
            if (kind == var)
                obj.adr = topScope.nextAdr++;

            return obj;
        }

        /**
         * Add a member object to the new data type
         */
        public Obj NewMemberObj(Obj obj, string name, int kind, int type) {
            Obj member = NewObj(name, kind, type, true);

            // Add new member var to end of linked list
            if (obj.locals == null) {
                obj.locals = member;
            }
            else {
                Obj curr = obj.locals;
                Obj last = null;
                while (curr != null) {
                    if (curr.name == member.name && obj.type == new_data_type)
                        parser.SemErr(member.name + " declared twice within new data type " + obj.name);

                    last = curr;
                    curr = curr.next;
                }
                last.next = member;
            }

            return member;
        }

        public Obj NewInstance(string new_data_type, string instance) {
            Obj newDataType = Find(new_data_type);

            // Create new instance of new data type.
            Obj obj = NewObj(instance, var, new_data_type_instance);

            // Copy members from new data type to instance.
            Obj member = newDataType.locals;
            while (member != null) {
                NewMemberObj(obj, member.name, member.kind, member.type);
                member = member.next;
            }

            return obj;
        }

        // Create a new constant with a value.
        public Obj NewConst(string name, int type, int value) {
            Obj obj = NewObj(name, constant, type);
            obj.value = value;

            return obj;
        }

        // Add extra necessary fields to predefined array object.
        public void CreateArray(Obj obj, int dims, int size1, int size2) {
            obj.dims = dims;
            obj.size1 = size1;
            obj.size2 = size2;

            if (dims == 1)
                topScope.nextAdr += size1 - 1;    // Allocate memory for 1D array.
            else if (dims == 2)
                topScope.nextAdr += (size1 * size2) - 1;    // Allocate memory for 2D array.
        }

        // search for name in open scopes and return its object node
        public Obj Find(string name) {
            Obj scope = topScope;
            while (scope != null) { // for all open scopes
                Obj obj = scope.locals;
                while (obj != null) { // for all objects in this scope
                    if (obj.name == name)
                        return obj;
                    obj = obj.next;
                }
                scope = scope.outer;
            }
            parser.SemErr(name + " is undeclared");
            return undefObj;
        }

        // Searches a new data type instance for a member variable
        public Obj FindMember(Obj instance, string name) {
            Obj member = instance.locals;
            while (member != null) {
                if (member.name == name)
                    return member;
                member = member.next;
            }

            return null; // Member var does not exist.
        }

    } // end SymbolTable
} // end namespace
