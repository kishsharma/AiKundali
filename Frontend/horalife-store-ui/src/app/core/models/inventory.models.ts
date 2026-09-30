export interface InventoryListItem {
  id: string;
  productId: string;
  variantId: string | null;
  productName: string;
  productSku: string;
  variantName: string | null;
  variantSku: string | null;
  stockQuantity: number;
  reservedQuantity: number;
  availableQuantity: number;
  reorderLevel: number;
  isLowStock: boolean;
  updatedAt: string;
}

export interface InventoryTransactionListItem {
  id: string;
  inventoryId: string;
  transactionType: string;
  quantityChange: number;
  referenceType: string | null;
  referenceId: string | null;
  notes: string | null;
  createdAt: string;
}

export interface InventoryAdjustRequest {
  quantityChange: number;
  reason?: string;
  transactionType?: 'stock_added' | 'manual_adjustment';
}

export interface InventoryReorderLevelRequest {
  reorderLevel: number;
}
