'use client';
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts';
import { fmtMoney, fmtPercent } from '@/lib/utils';

type Props = { revenue: number; cost: number };

export function ProfitDonut({ revenue, cost }: Props) {
  const profit = Math.max(0, revenue - cost);
  const margin = revenue > 0 ? (profit / revenue) * 100 : 0;
  const data = [
    { name: 'Carrier cost', value: cost, color: '#94a3b8' },
    { name: 'Profit', value: profit, color: '#16a34a' },
  ];

  return (
    <div className="relative h-64">
      <ResponsiveContainer width="100%" height="100%">
        <PieChart>
          <Pie
            data={data}
            cx="50%"
            cy="50%"
            innerRadius={70}
            outerRadius={100}
            paddingAngle={2}
            dataKey="value"
            stroke="none"
          >
            {data.map((d) => (
              <Cell key={d.name} fill={d.color} />
            ))}
          </Pie>
          <Tooltip
            formatter={(v: any, n: any) => [fmtMoney(Number(v)), n]}
            contentStyle={{ borderRadius: 8, fontSize: 12 }}
          />
        </PieChart>
      </ResponsiveContainer>
      <div className="absolute inset-0 grid place-items-center pointer-events-none">
        <div className="text-center">
          <div className="text-xs text-muted-foreground">Profit</div>
          <div className="text-xl font-semibold">{fmtMoney(profit)}</div>
          <div className="text-xs text-emerald-600">{fmtPercent(margin)} margin</div>
        </div>
      </div>
    </div>
  );
}
