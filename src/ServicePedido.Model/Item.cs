using System;
using System.Collections.Generic;
using System.Text;

namespace ServicePedido.Model
{
    public class Item
    {
        public required string NomeProduto { get; set; }
        public required int Quantidade { get; set; }
        public required decimal PrecoUnitario { get; set; }
    }
}
