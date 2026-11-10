export interface MedicalInventoryItemDto {
  id: string;
  clinicId: string;
  clinicName?: string;
  itemName: string;
  itemNameAr?: string;
  category: 'Consumables' | 'Pharmaceuticals' | 'Equipment' | 'Supplies' | string;
  currentStock: number;
  minimumThreshold: number;
  unit: string;
  unitPrice: number;
  batchNumber?: string;
  expiryDate?: string;
  isLowStock: boolean;
  isExpired: boolean;
  lastUpdatedUtc: string;
}

export interface CreateInventoryItemRequest {
  clinicId: string;
  itemName: string;
  itemNameAr?: string;
  category: string;
  initialStock: number;
  minimumThreshold: number;
  unit: string;
  unitPrice: number;
  batchNumber?: string;
  expiryDate?: string;
}
