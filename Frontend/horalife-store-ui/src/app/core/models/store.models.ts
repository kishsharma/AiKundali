export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface Category {
  id: string;
  parentId?: string | null;
  name: string;
  slug: string;
  description?: string | null;
  displayOrder: number;
  productCount: number;
}

export interface ProductListItem {
  id: string;
  sku: string;
  slug: string;
  name: string;
  shortDescription?: string | null;
  productType: string;
  displayPrice: number;
  compareAtPrice?: number | null;
  currency: string;
  primaryImageUrl?: string | null;
  primaryImageAltText?: string | null;
  requiresConsultation: boolean;
  isFeatured: boolean;
  inStock: boolean;
  availableQuantity: number;
}

export interface ProductVariant {
  id: string;
  sku: string;
  name: string;
  price: number;
  compareAtPrice?: number | null;
  weightGrams?: number | null;
  attributes: Record<string, unknown>;
  availableQuantity: number;
  isActive: boolean;
}

export interface ProductImage {
  id: string;
  variantId?: string | null;
  imageUrl: string;
  altText?: string | null;
  displayOrder: number;
  isPrimary: boolean;
}

export interface ProductDetail {
  id: string;
  categoryId: string;
  categoryName: string;
  categorySlug: string;
  sku: string;
  slug: string;
  name: string;
  shortDescription?: string | null;
  description?: string | null;
  productType: string;
  basePrice: number;
  compareAtPrice?: number | null;
  currency: string;
  attributes: Record<string, unknown>;
  requiresConsultation: boolean;
  isFeatured: boolean;
  inStock: boolean;
  availableQuantity: number;
  variants: ProductVariant[];
  images: ProductImage[];
}

export interface CartItem {
  id: string;
  productId: string;
  variantId?: string | null;
  productSlug: string;
  productName: string;
  variantName?: string | null;
  sku?: string | null;
  imageUrl?: string | null;
  imageAltText?: string | null;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  currency: string;
  availableQuantity: number;
  requiresConsultation: boolean;
}

export interface Cart {
  id?: string | null;
  currency: string;
  totalQuantity: number;
  subtotal: number;
  items: CartItem[];
}

export interface CustomerAddress {
  id: string;
  addressType: string;
  fullName: string;
  phone: string;
  addressLine1: string;
  addressLine2?: string | null;
  city: string;
  state: string;
  postalCode: string;
  countryCode: string;
  isDefault: boolean;
}

export interface SaveAddressRequest {
  addressType: 'shipping' | 'billing';
  fullName: string;
  phone: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  state: string;
  postalCode: string;
  countryCode: string;
  isDefault: boolean;
}

export interface CheckoutAddress {
  id: string;
  addressType: string;
  fullName: string;
  phone: string;
  addressLine1: string;
  addressLine2?: string | null;
  city: string;
  state: string;
  postalCode: string;
  countryCode: string;
}

export interface CheckoutPreviewItem {
  cartItemId: string;
  productId: string;
  variantId?: string | null;
  productSlug: string;
  productName: string;
  variantName?: string | null;
  sku?: string | null;
  imageUrl?: string | null;
  cartUnitPrice: number;
  currentUnitPrice: number;
  priceChanged: boolean;
  quantity: number;
  lineTotal: number;
  availableQuantity: number;
  isAvailable: boolean;
  requiresConsultation: boolean;
  warnings: string[];
}

export interface CheckoutPreview {
  cartId: string;
  currency: string;
  shippingAddress: CheckoutAddress;
  billingAddress: CheckoutAddress;
  billingSameAsShipping: boolean;
  items: CheckoutPreviewItem[];
  totalQuantity: number;
  subtotal: number;
  shippingCharge: number;
  discountAmount: number;
  taxAmount: number;
  grandTotal: number;
  estimatedDeliveryMinDays: number;
  estimatedDeliveryMaxDays: number;
  requiresConsultation: boolean;
  canCheckout: boolean;
  warnings: string[];
  generatedAt: string;
}

export interface CheckoutRevalidationIssue {
  code: string;
  message: string;
}

export interface CheckoutRevalidation {
  cartId: string;
  currency: string;
  isValid: boolean;
  requiresNewPreview: boolean;
  cartPricesUpdated: boolean;
  previousSubtotal: number;
  currentSubtotal: number;
  issues: CheckoutRevalidationIssue[];
}

export interface CreatedOrder {
  orderId: string;
  orderNumber: number;
  cartId: string;
  userId: string;
  customerEmail: string;
  customerPhone?: string | null;
  orderStatus: string;
  paymentStatus: string;
  subtotalAmount: number;
  discountAmount: number;
  shippingAmount: number;
  taxAmount: number;
  totalAmount: number;
  currency: string;
  orderSource: string;
  notes?: string | null;
  orderItemsCreated: boolean;
  placedAt: string;
}

export interface ConvertedCart {
  cartId: string;
  orderId: string;
  orderNumber: number;
  previousCartStatus: string;
  currentCartStatus: string;
  orderStatus: string;
  paymentStatus: string;
  orderItemsCount: number;
  inventoryReserved: boolean;
  cartConverted: boolean;
  convertedAt: string;
}

export interface PlacedOrder {
  order: CreatedOrder;
  converted: ConvertedCart;
}

export interface ProductQuery {
  category?: string;
  search?: string;
  productType?: string;
  isFeatured?: boolean;
  pageNumber: number;
  pageSize: number;
  sortBy: string;
  sortDirection: string;
}

export const PRODUCT_TYPES = [
  { value: 'gemstone', label: 'Gemstone' },
  { value: 'yantra', label: 'Yantra' },
  { value: 'rudraksha', label: 'Rudraksha' },
  { value: 'puja_item', label: 'Puja item' },
  { value: 'remedy_kit', label: 'Remedy kit' },
  { value: 'other', label: 'Other' }
] as const;

export function productTypeLabel(value: string): string {
  return PRODUCT_TYPES.find((type) => type.value === value)?.label ?? value;
}
