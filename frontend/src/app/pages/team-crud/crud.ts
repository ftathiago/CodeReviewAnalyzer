import { Component, OnInit, signal, ViewChild } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Table, TableModule } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { ToastModule } from 'primeng/toast';
import { ToolbarModule } from 'primeng/toolbar';
import { RatingModule } from 'primeng/rating';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { RadioButtonModule } from 'primeng/radiobutton';
import { InputNumberModule } from 'primeng/inputnumber';
import { DialogModule } from 'primeng/dialog';
import { TagModule } from 'primeng/tag';
import { InputIconModule } from 'primeng/inputicon';
import { IconFieldModule } from 'primeng/iconfield';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { Product, ProductService } from '../service/product.service';
import { TeamsRepositoryService } from '../service/teamsRepository/teams-repository.service';
import { Team } from '../service/report/models/team';

interface Column {
    field: string;
    header: string;
    customExportHeader?: string;
}

interface ExportColumn {
    title: string;
    dataKey: string;
}

@Component({
    selector: 'app-crud',
    standalone: true,
    imports: [
        CommonModule,
        TableModule,
        FormsModule,
        ButtonModule,
        RippleModule,
        ToastModule,
        ToolbarModule,
        RatingModule,
        InputTextModule,
        TextareaModule,
        SelectModule,
        RadioButtonModule,
        InputNumberModule,
        DialogModule,
        TagModule,
        InputIconModule,
        IconFieldModule,
        ConfirmDialogModule
    ],
    templateUrl: './crud.html',
    providers: [MessageService, ProductService, ConfirmationService]
})
export class TeamCrud implements OnInit {
    teamDialog: boolean = false;

    products = signal<Product[]>([]);
    teams = signal<Team[]>([]);

    product!: Product;
    team!: Team;

    selectedProducts!: Product[] | null;
    selectedTeams!: Team[] | null;

    submitted: boolean = false;

    statuses!: any[];

    @ViewChild('dt') dt!: Table;

    exportColumns!: ExportColumn[];

    cols!: Column[];

    constructor(
        private productService: ProductService,
        private teamRepository: TeamsRepositoryService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    exportCSV() {
        this.dt.exportCSV();
    }

    ngOnInit() {
        this.loadDemoData();
    }

    loadDemoData() {
        this.teamRepository
            .getTeamsByDescription('*', {
                page: 0,
                size: 30,
                order: 'name'
            })
            .subscribe({
                next: (response) => {
                    this.teams.set(response.data);
                },
                error: (err) => console.log(err)
            });

        this.statuses = [
            { label: 'INSTOCK', value: 'instock' },
            { label: 'LOWSTOCK', value: 'lowstock' },
            { label: 'OUTOFSTOCK', value: 'outofstock' }
        ];

        this.cols = [
            {
                field: 'externalId',
                header: 'External Id',
                customExportHeader: 'External Id'
            },
            { field: 'name', header: 'Name' },
            { field: 'description', header: 'Description' },
            { field: 'active', header: 'Active' }
        ];

        this.exportColumns = this.cols.map((col) => ({
            title: col.header,
            dataKey: col.field
        }));
    }

    onGlobalFilter(table: Table, event: Event) {
        console.log(`${(event.target as HTMLInputElement).value}`);

        table.filterGlobal(
            (event.target as HTMLInputElement).value,
            'contains'
        );
    }

    openNew() {
        this.team = {};
        this.submitted = false;
        this.teamDialog = true;
    }

    editProduct(team: Team) {
        this.team = { ...team };
        this.teamDialog = true;
    }

    deleteSelectedProducts() {
        this.confirmationService.confirm({
            message: 'Are you sure you want to delete the selected teams?',
            header: 'Confirm',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.teams.set(
                    this.teams().filter(
                        (val) => !this.selectedTeams?.includes(val)
                    )
                );
                this.selectedTeams = null;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Successful',
                    detail: 'Teams Deleted',
                    life: 3000
                });
            }
        });
    }

    hideDialog() {
        this.teamDialog = false;
        this.submitted = false;
    }

    deleteProduct(team: Team) {
        this.confirmationService.confirm({
            message: 'Are you sure you want to delete ' + team.name + '?',
            header: 'Confirm',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.teams.set(
                    this.teams().filter(
                        (val) => val.externalId !== team.externalId
                    )
                );
                this.team = {};
                this.messageService.add({
                    severity: 'success',
                    summary: 'Successful',
                    detail: 'Team Deleted',
                    life: 3000
                });
            }
        });
    }

    findIndexById(id: string): number {
        let index = -1;
        for (let i = 0; i < this.teams().length; i++) {
            if (this.teams()[i].externalId === id) {
                index = i;
                break;
            }
        }

        return index;
    }

    createId(): string {
        let id = '';
        var chars =
            'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
        for (var i = 0; i < 5; i++) {
            id += chars.charAt(Math.floor(Math.random() * chars.length));
        }
        return id;
    }

    getStatus(status: boolean): string {
        if (status) return 'Active';
        else return 'Deactivated';
    }

    getSeverity(status: boolean) {
        if (status) return 'success';
        else return 'danger';
    }

    saveProduct() {
        this.submitted = true;
        let _teams = this.teams();
        if (this.team.name?.trim()) {
            if (this.team.externalId) {
                _teams[this.findIndexById(this.team.externalId)] = this.team;
                this.teams.set([..._teams]);
                this.messageService.add({
                    severity: 'success',
                    summary: 'Successful',
                    detail: 'Product Updated',
                    life: 3000
                });
            } else {
                this.team.externalId = uuidv4();
                this.messageService.add({
                    severity: 'success',
                    summary: 'Successful',
                    detail: 'Product Created',
                    life: 3000
                });
                this.teams.set([..._teams, this.team]);
            }

            this.teamDialog = false;
            this.team = {};
        }
    }
}

function uuidv4() {
    return '10000000-1000-4000-8000-100000000000'.replace(/[018]/g, (c) =>
        (
            +c ^
            (crypto.getRandomValues(new Uint8Array(1))[0] & (15 >> (+c / 4)))
        ).toString(16)
    );
}
